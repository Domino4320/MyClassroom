(function () {
    function parseTagTokens(query) {
        return query
            .split(",")
            .map((part) => part.trim().toLowerCase())
            .filter((part) => part.length > 0);
    }

    function initTeacherCatalogFilters() {
        const nameInput = document.getElementById("teacherNameSearch");
        const specFilter = document.getElementById("teacherSpecFilter");
        const tagsInput = document.getElementById("teacherTagsSearch");
        const items = document.querySelectorAll(".teacher-catalog-item");
        const noResults = document.getElementById("teachersNoResults");

        if (!items.length) return;

        function applyFilter() {
            const nameQuery = nameInput ? nameInput.value.trim().toLowerCase() : "";
            const specValue = specFilter ? specFilter.value.trim().toLowerCase() : "";
            const tagTokens = tagsInput ? parseTagTokens(tagsInput.value) : [];

            let foundCount = 0;

            items.forEach((item) => {
                const name = item.getAttribute("data-name") || "";
                const specialization = item.getAttribute("data-specialization") || "";
                const tags = item.getAttribute("data-tags") || "";

                const matchesName = nameQuery === "" || name.includes(nameQuery);
                const matchesSpec =
                    specValue === "" || specialization === specValue;
                const matchesTags =
                    tagTokens.length === 0 ||
                    tagTokens.every((token) => tags.includes(token));

                if (matchesName && matchesSpec && matchesTags) {
                    item.classList.remove("hidden");
                    foundCount++;
                } else {
                    item.classList.add("hidden");
                }
            });

            if (noResults) {
                noResults.style.display = foundCount === 0 ? "block" : "none";
            }
        }

        if (nameInput) nameInput.addEventListener("input", applyFilter);
        if (specFilter) specFilter.addEventListener("change", applyFilter);
        if (tagsInput) tagsInput.addEventListener("input", applyFilter);
    }

    window.initTeacherCatalogFilters = initTeacherCatalogFilters;
})();
