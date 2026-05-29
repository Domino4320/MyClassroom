using System.Text.Json;

namespace WebApplication2.Models
{
    public static class InteractiveStepHelper
    {
        public class InteractiveConfig
        {
            public string Kind { get; set; } = "match";
            public string? Instruction { get; set; }
            public List<MatchPair>? Pairs { get; set; }
            public List<string>? Items { get; set; }
            public List<TrueFalseStatement>? Statements { get; set; }
        }

        public class MatchPair
        {
            public string Left { get; set; } = "";
            public string Right { get; set; } = "";
        }

        public class TrueFalseStatement
        {
            public string Text { get; set; } = "";
            public bool IsTrue { get; set; }
        }

        public static InteractiveConfig? ParseConfig(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var config = JsonSerializer.Deserialize<InteractiveConfig>(json, JsonOptions);
                return config == null ? null : NormalizeConfig(config);
            }
            catch
            {
                return null;
            }
        }

        public static string NormalizeConfigJson(string? json)
        {
            var config = ParseConfig(json);
            return config == null ? (json ?? "") : JsonSerializer.Serialize(config, JsonOptions);
        }

        private static InteractiveConfig NormalizeConfig(InteractiveConfig config)
        {
            var kind = (config.Kind ?? "match").ToLowerInvariant();
            if (kind == "order") kind = "sequence";
            config.Kind = kind;

            var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["match"] = "Сопоставьте термины с определениями",
                ["sequence"] = "Расставьте элементы в правильном порядке",
                ["truefalse"] = "Определите, какие утверждения верны, а какие нет"
            };

            var instruction = (config.Instruction ?? "").Trim();
            if (string.IsNullOrWhiteSpace(instruction) || defaults.Values.Contains(instruction))
                config.Instruction = defaults.GetValueOrDefault(kind, defaults["match"]);

            return config;
        }

        public static bool TryValidateConfig(string? configJson, out string? error)
        {
            error = null;
            var config = ParseConfig(configJson);
            if (config == null)
            {
                error = "Некорректная конфигурация интерактивного задания.";
                return false;
            }

            var kind = (config.Kind ?? "match").ToLowerInvariant();
            if (kind == "order") kind = "sequence";

            if (string.IsNullOrWhiteSpace(config.Instruction))
            {
                error = "Введите инструкцию для студента.";
                return false;
            }

            if (kind == "match")
            {
                if (config.Pairs == null || config.Pairs.Count == 0)
                {
                    error = "Добавьте хотя бы одну пару «термин → определение».";
                    return false;
                }

                if (config.Pairs.Any(p => string.IsNullOrWhiteSpace(p.Left) || string.IsNullOrWhiteSpace(p.Right)))
                {
                    error = "Заполните все пары полностью.";
                    return false;
                }

                return true;
            }

            if (kind == "sequence")
            {
                var items = config.Items?.Where(i => !string.IsNullOrWhiteSpace(i)).ToList() ?? new List<string>();
                if (items.Count < 2)
                {
                    error = "Добавьте минимум 2 элемента для упорядочивания.";
                    return false;
                }

                return true;
            }

            if (kind == "truefalse")
            {
                var statements = config.Statements?.Where(s => !string.IsNullOrWhiteSpace(s.Text)).ToList() ?? new List<TrueFalseStatement>();
                if (statements.Count == 0)
                {
                    error = "Добавьте хотя бы одно утверждение.";
                    return false;
                }

                return true;
            }

            error = "Неизвестный тип интерактивного задания.";
            return false;
        }

        public static bool TryValidate(string? configJson, string? answerJson, out string? error)
        {
            error = null;
            var config = ParseConfig(configJson);
            if (config == null)
            {
                error = "Некорректная конфигурация задания.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(answerJson))
            {
                error = "Выполните задание полностью.";
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(answerJson);
                var root = doc.RootElement;
                var kind = (config.Kind ?? "match").ToLowerInvariant();

                if (kind is "order" or "sequence")
                    return ValidateSequence(config, root, out error);

                if (kind == "truefalse")
                    return ValidateTrueFalse(config, root, out error);

                return ValidateMatch(config, root, out error);
            }
            catch
            {
                error = "Неверный формат ответа.";
                return false;
            }
        }

        private static bool ValidateMatch(InteractiveConfig config, JsonElement root, out string? error)
        {
            error = null;
            if (config.Pairs == null || config.Pairs.Count == 0)
            {
                error = "Задание не настроено.";
                return false;
            }

            if (!root.TryGetProperty("matches", out var matchesEl) || matchesEl.ValueKind != JsonValueKind.Object)
            {
                error = "Сопоставьте все пары.";
                return false;
            }

            foreach (var pair in config.Pairs)
            {
                if (!matchesEl.TryGetProperty(pair.Left, out var rightEl))
                {
                    error = "Сопоставьте все пары.";
                    return false;
                }

                if (!string.Equals(Normalize(rightEl.GetString()), Normalize(pair.Right), StringComparison.Ordinal))
                {
                    error = "Есть неверные сопоставления. Проверьте пары.";
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateSequence(InteractiveConfig config, JsonElement root, out string? error)
        {
            error = null;
            if (config.Items == null || config.Items.Count == 0)
            {
                error = "Задание не настроено.";
                return false;
            }

            if (!root.TryGetProperty("order", out var orderEl) || orderEl.ValueKind != JsonValueKind.Array)
            {
                error = "Неверный формат ответа.";
                return false;
            }

            var userOrder = orderEl.EnumerateArray().Select(x => Normalize(x.GetString())).ToList();
            var expected = config.Items.Select(Normalize).ToList();

            if (userOrder.Count != expected.Count || !userOrder.SequenceEqual(expected))
            {
                error = "Порядок элементов неверный. Попробуйте ещё раз.";
                return false;
            }

            return true;
        }

        private static bool ValidateTrueFalse(InteractiveConfig config, JsonElement root, out string? error)
        {
            error = null;
            if (config.Statements == null || config.Statements.Count == 0)
            {
                error = "Задание не настроено.";
                return false;
            }

            if (!root.TryGetProperty("answers", out var answersEl) || answersEl.ValueKind != JsonValueKind.Array)
            {
                error = "Отметьте все утверждения.";
                return false;
            }

            var userAnswers = answersEl.EnumerateArray().ToList();
            if (userAnswers.Count != config.Statements.Count)
            {
                error = "Отметьте все утверждения.";
                return false;
            }

            for (var i = 0; i < config.Statements.Count; i++)
            {
                var el = userAnswers[i];
                if (el.ValueKind == JsonValueKind.Null)
                {
                    error = "Отметьте все утверждения как «Верно» или «Неверно».";
                    return false;
                }

                var userVal = el.ValueKind == JsonValueKind.True;
                if (userVal != config.Statements[i].IsTrue)
                {
                    error = "Не все утверждения отмечены верно. Перечитайте материал и попробуйте снова.";
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string? s) => (s ?? "").Trim().ToLowerInvariant();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
