(function () {
    function initCourseListFilters(searchInputId) {
        const searchInput = document.getElementById(searchInputId);
        const categoryFilter = document.getElementById("categoryFilter");
        const ratingFilter = document.getElementById("ratingFilter");
        const items = document.querySelectorAll(".course-item");
        const noResults = document.getElementById("noResults");

        if (!searchInput || !items.length) return;

        function applyFilters() {
            const query = searchInput.value.toLowerCase().trim();
            const category = categoryFilter ? categoryFilter.value : "";
            const minRating = ratingFilter && ratingFilter.value !== ""
                ? parseFloat(ratingFilter.value)
                : null;

            let foundCount = 0;

            items.forEach((item) => {
                const title = item.getAttribute("data-title") || "";
                const itemCategory = item.getAttribute("data-category") || "";
                const rating = parseFloat(item.getAttribute("data-rating") || "0");

                const matchesTitle = query === "" || title.includes(query);
                const matchesCategory = category === "" || itemCategory === category;
                const matchesRating = minRating === null || rating >= minRating;

                if (matchesTitle && matchesCategory && matchesRating) {
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

        searchInput.addEventListener("input", applyFilters);
        if (categoryFilter) categoryFilter.addEventListener("change", applyFilters);
        if (ratingFilter) ratingFilter.addEventListener("change", applyFilters);
    }

    window.initCourseListFilters = initCourseListFilters;
})();
