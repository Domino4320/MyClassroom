(function () {
    function initCourseListFilters(searchInputId, authorSearchInputId) {
        const searchInput = document.getElementById(searchInputId);
        const authorSearchInput = document.getElementById(authorSearchInputId || "authorSearch");
        const categoryFilter = document.getElementById("categoryFilter");
        const ratingFilter = document.getElementById("ratingFilter");
        const dateSortFilter = document.getElementById("dateSortFilter");
        const recPercentFilter = document.getElementById("recPercentFilter");
        const grid = document.getElementById("coursesGrid");
        const items = grid ? Array.from(grid.querySelectorAll(".course-item")) : [];
        const noResults = document.getElementById("noResults");

        if (!searchInput || !items.length || !grid) return;

        function sortByDate() {
            const order = dateSortFilter && dateSortFilter.value ? dateSortFilter.value : "desc";

            const sorted = [...items].sort((a, b) => {
                const da = Number(a.getAttribute("data-created") || "0");
                const db = Number(b.getAttribute("data-created") || "0");
                return order === "asc" ? da - db : db - da;
            });

            sorted.forEach((el) => grid.appendChild(el));
        }

        function applyFilters() {
            const query = searchInput.value.toLowerCase().trim();
            const authorQuery = authorSearchInput
                ? authorSearchInput.value.toLowerCase().trim()
                : "";
            const category = categoryFilter ? categoryFilter.value : "";
            const minRating =
                ratingFilter && ratingFilter.value !== ""
                    ? parseFloat(ratingFilter.value)
                    : null;
            const minRec =
                recPercentFilter && recPercentFilter.value !== ""
                    ? parseInt(recPercentFilter.value, 10)
                    : null;

            let foundCount = 0;

            items.forEach((item) => {
                const title = item.getAttribute("data-title") || "";
                const author = item.getAttribute("data-author") || "";
                const itemCategory = item.getAttribute("data-category") || "";
                const rating = parseFloat(item.getAttribute("data-rating") || "0");
                const rec = parseInt(item.getAttribute("data-rec") || "0", 10);

                const matchesTitle = query === "" || title.includes(query);
                const matchesAuthor =
                    authorQuery === "" || author.includes(authorQuery);
                const matchesCategory = category === "" || itemCategory === category;
                const matchesRating = minRating === null || rating >= minRating;
                const matchesRec =
                    minRec === null || Number.isNaN(minRec) || rec >= minRec;

                if (
                    matchesTitle &&
                    matchesAuthor &&
                    matchesCategory &&
                    matchesRating &&
                    matchesRec
                ) {
                    item.classList.remove("hidden");
                    foundCount++;
                } else {
                    item.classList.add("hidden");
                }
            });

            sortByDate();

            if (noResults) {
                noResults.style.display = foundCount === 0 ? "block" : "none";
            }
        }

        searchInput.addEventListener("input", applyFilters);
        if (authorSearchInput) authorSearchInput.addEventListener("input", applyFilters);
        if (categoryFilter) categoryFilter.addEventListener("change", applyFilters);
        if (ratingFilter) ratingFilter.addEventListener("change", applyFilters);
        if (dateSortFilter) dateSortFilter.addEventListener("change", applyFilters);
        if (recPercentFilter) {
            recPercentFilter.addEventListener("input", applyFilters);
            recPercentFilter.addEventListener("change", applyFilters);
        }

        applyFilters();
    }

    window.initCourseListFilters = initCourseListFilters;
})();
