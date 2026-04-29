using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApplication2.Services
{
    public class CertificatePdfService
    {
        static CertificatePdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateCourseCertificate(string userName, string courseTitle, DateTime issuedAtUtc)
        {
            var issuedAt = issuedAtUtc.ToLocalTime().ToString("dd.MM.yyyy");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4.Landscape());
                    page.PageColor(Colors.Blue.Lighten5);

                    page.Content().Padding(18).Border(2).BorderColor(Colors.Blue.Lighten2).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(180).AlignRight().AlignMiddle().Border(2).BorderColor(Colors.Blue.Medium)
                                .Background(Colors.Blue.Lighten5).PaddingVertical(8).PaddingHorizontal(12).Column(logo =>
                                {
                                    logo.Item().AlignCenter().Text("MYCLASSROOM")
                                        .FontFamily("DejaVu Sans")
                                        .FontSize(11)
                                        .SemiBold()
                                        .FontColor(Colors.Blue.Darken3);
                                    logo.Item().AlignCenter().Text("ACADEMY")
                                        .FontFamily("DejaVu Sans")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken1);
                                });
                        });

                        col.Item().AlignCenter().Text("СЕРТИФИКАТ")
                            .FontFamily("DejaVu Sans")
                            .FontSize(42)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken3);

                        col.Item().AlignCenter().Text("о прохождении курса")
                            .FontFamily("DejaVu Sans")
                            .FontSize(22)
                            .FontColor(Colors.Grey.Darken2);

                        col.Item().PaddingTop(30).AlignCenter().Text($"Настоящим подтверждается, что {userName}")
                            .FontFamily("DejaVu Sans")
                            .FontSize(20)
                            .FontColor(Colors.Black);

                        col.Item().AlignCenter().Text($"успешно завершил(а) курс: \"{courseTitle}\"")
                            .FontFamily("DejaVu Sans")
                            .FontSize(20)
                            .SemiBold()
                            .FontColor(Colors.Black);

                        col.Item().PaddingTop(30).AlignCenter().Text($"Дата выдачи: {issuedAt}")
                            .FontFamily("DejaVu Sans")
                            .FontSize(14)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item().PaddingTop(20).AlignCenter().Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(170).AlignCenter().Column(seal =>
                            {
                                seal.Item().AlignCenter().Width(120).Height(120).Layers(layers =>
                                {
                                    layers.PrimaryLayer().Svg(@"
<svg viewBox='0 0 120 120' xmlns='http://www.w3.org/2000/svg'>
  <circle cx='60' cy='60' r='54' fill='none' stroke='#1d4ed8' stroke-width='4'/>
  <circle cx='60' cy='60' r='44' fill='none' stroke='#60a5fa' stroke-width='2' stroke-dasharray='4 4'/>
  <text x='60' y='52' text-anchor='middle' font-size='13' font-family='DejaVu Sans' font-weight='700' fill='#1e3a8a'>MC</text>
  <text x='60' y='72' text-anchor='middle' font-size='9' font-family='DejaVu Sans' fill='#334155'>Verified</text>
</svg>");
                                });
                                seal.Item().PaddingTop(6).AlignCenter().Text("Официальная печать")
                                    .FontFamily("DejaVu Sans")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                            row.RelativeItem();
                        });
                    });
                });
            }).GeneratePdf();
        }
    }
}
