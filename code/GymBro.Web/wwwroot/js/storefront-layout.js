(function () {
    function onReady(callback) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", callback);
            return;
        }

        callback();
    }

    function initMobileNavigation() {
        var navbarToggler = document.querySelector(".navbar-toggler");
        var navbarCollapse = document.querySelector(".navbar-collapse");

        if (!navbarToggler || !navbarCollapse || typeof bootstrap === "undefined") {
            return;
        }

        document.addEventListener("click", function (event) {
            var isClickInside = navbarCollapse.contains(event.target) || navbarToggler.contains(event.target);
            if (!isClickInside && navbarCollapse.classList.contains("show")) {
                bootstrap.Collapse.getOrCreateInstance(navbarCollapse).hide();
            }
        });

        navbarCollapse.querySelectorAll(".nav-link").forEach(function (link) {
            link.addEventListener("click", function () {
                if (window.innerWidth < 992) {
                    bootstrap.Collapse.getOrCreateInstance(navbarCollapse).hide();
                }
            });
        });
    }

    function initDesktopShopLink() {
        var shopLink = document.querySelector(".js-shop-nav-link");
        if (!shopLink) {
            return;
        }

        shopLink.addEventListener("click", function (event) {
            if (window.innerWidth >= 992) {
                event.preventDefault();
                window.location.href = shopLink.href;
            }
        });
    }

    function initSearchSuggestions() {
        var searchInput = document.getElementById("searchInput");
        var suggestionsContainer = document.getElementById("searchSuggestions");
        var suggestionsList = document.getElementById("suggestionsList");
        var suggestionsUrl = document.body.getAttribute("data-search-suggestions-url");
        var searchTimeout;
        var currentFocus = -1;

        if (!searchInput || !suggestionsContainer || !suggestionsList || !suggestionsUrl) {
            return;
        }

        searchInput.addEventListener("input", function () {
            var term = this.value.trim();
            window.clearTimeout(searchTimeout);

            if (term.length < 2) {
                hideSuggestions();
                return;
            }

            searchTimeout = window.setTimeout(function () {
                fetchSuggestions(term);
            }, 250);
        });

        searchInput.addEventListener("keydown", function (event) {
            var suggestions = suggestionsContainer.querySelectorAll(".suggestion-item");

            if (event.key === "ArrowDown") {
                event.preventDefault();
                currentFocus = currentFocus < suggestions.length - 1 ? currentFocus + 1 : 0;
                updateFocus(suggestions);
            } else if (event.key === "ArrowUp") {
                event.preventDefault();
                currentFocus = currentFocus > 0 ? currentFocus - 1 : suggestions.length - 1;
                updateFocus(suggestions);
            } else if (event.key === "Enter") {
                if (currentFocus >= 0 && suggestions[currentFocus]) {
                    event.preventDefault();
                    selectSuggestion(suggestions[currentFocus].textContent);
                }
            } else if (event.key === "Escape") {
                hideSuggestions();
            }
        });

        document.addEventListener("click", function (event) {
            if (!event.target.closest(".search-form")) {
                hideSuggestions();
            }
        });

        function fetchSuggestions(term) {
            fetch(suggestionsUrl + "?term=" + encodeURIComponent(term))
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    displaySuggestions(data && data.suggestions ? data.suggestions : []);
                })
                .catch(function () {
                    hideSuggestions();
                });
        }

        function displaySuggestions(suggestions) {
            if (!suggestions.length) {
                hideSuggestions();
                return;
            }

            suggestionsList.innerHTML = "";
            suggestions.forEach(function (suggestion, index) {
                var item = document.createElement("div");
                item.className = "suggestion-item px-3 py-2";
                item.textContent = suggestion;
                item.addEventListener("click", function () {
                    selectSuggestion(suggestion);
                });
                item.addEventListener("mouseenter", function () {
                    currentFocus = index;
                    updateFocus(suggestionsContainer.querySelectorAll(".suggestion-item"));
                });
                suggestionsList.appendChild(item);
            });

            suggestionsContainer.classList.add("is-visible");
            currentFocus = -1;
        }

        function selectSuggestion(suggestion) {
            searchInput.value = suggestion;
            hideSuggestions();
            if (searchInput.form) {
                searchInput.form.submit();
            }
        }

        function updateFocus(suggestions) {
            suggestions.forEach(function (item, index) {
                item.classList.toggle("active", index === currentFocus);
            });
        }

        function hideSuggestions() {
            suggestionsContainer.classList.remove("is-visible");
            suggestionsList.innerHTML = "";
            currentFocus = -1;
        }
    }

    function initQuickView() {
        var modalElement = document.getElementById("quickViewModal");
        var content = document.getElementById("quickViewContent");
        var quickViewUrl = document.body.getAttribute("data-quick-view-url");

        if (!modalElement || !content || !quickViewUrl || typeof bootstrap === "undefined") {
            return;
        }

        var modal = bootstrap.Modal.getOrCreateInstance(modalElement);

        function loadQuickView(productId) {
            content.innerHTML = [
                '<div class="quick-view-state text-center py-5">',
                '    <div class="spinner-border text-primary" role="status">',
                '        <span class="visually-hidden">Đang tải...</span>',
                "    </div>",
                '    <p class="mt-3 mb-0">Đang tải thông tin sản phẩm...</p>',
                "</div>"
            ].join("");

            modal.show();

            fetch(quickViewUrl + "?id=" + encodeURIComponent(productId))
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error("Quick view failed");
                    }
                    return response.text();
                })
                .then(function (html) {
                    content.innerHTML = html;
                })
                .catch(function () {
                    content.innerHTML = [
                        '<div class="quick-view-state text-center py-5">',
                        '    <i class="bi bi-exclamation-triangle text-danger quick-view-status-icon"></i>',
                        '    <p class="mt-3 mb-0 text-danger">Không thể tải thông tin sản phẩm.</p>',
                        "</div>"
                    ].join("");
                });
        }

        document.addEventListener("click", function (event) {
            var trigger = event.target.closest(".js-quick-view-trigger");
            if (!trigger) {
                return;
            }

            event.preventDefault();
            var productId = trigger.getAttribute("data-product-id");
            if (productId) {
                loadQuickView(productId);
            }
        });
    }

    function initImageFallbacks(root) {
        var scope = root || document;

        scope.querySelectorAll(".js-image-fallback").forEach(function (image) {
            if (image.dataset.fallbackBound === "true") {
                return;
            }

            image.dataset.fallbackBound = "true";

            function showFallback() {
                var fallbackSelector = image.getAttribute("data-fallback-selector");
                var container = image.closest(".product-detail-image-panel") || image.parentElement;
                var fallback = fallbackSelector ? container.querySelector(fallbackSelector) : null;

                image.style.display = "none";
                if (fallback) {
                    fallback.classList.remove("is-hidden");
                }
            }

            image.addEventListener("error", showFallback);

            if (image.complete && image.naturalWidth === 0) {
                showFallback();
            }
        });
    }

    function initProductDetail() {
        var detailPage = document.querySelector(".product-detail-page");
        if (!detailPage) {
            return;
        }

        initImageFallbacks(detailPage);

        var quantityInput = detailPage.querySelector("#quantity");
        detailPage.querySelectorAll(".js-detail-quantity-btn").forEach(function (button) {
            button.addEventListener("click", function () {
                if (!quantityInput) {
                    return;
                }

                var currentValue = parseInt(quantityInput.value, 10);
                if (isNaN(currentValue) || currentValue < 1) {
                    currentValue = 1;
                }

                if (button.getAttribute("data-direction") === "decrease") {
                    quantityInput.value = Math.max(1, currentValue - 1);
                } else {
                    quantityInput.value = currentValue + 1;
                }
            });
        });

        var wishlistButton = detailPage.querySelector(".js-wishlist-toggle");
        var toggleUrl = detailPage.getAttribute("data-wishlist-toggle-url");
        var loginUrl = detailPage.getAttribute("data-login-url");

        if (!wishlistButton || !toggleUrl || typeof window.jQuery === "undefined") {
            return;
        }

        wishlistButton.addEventListener("click", function () {
            var productId = wishlistButton.getAttribute("data-product-id");
            var icon = wishlistButton.querySelector("i");

            window.jQuery.post(toggleUrl, { productId: productId }, function (response) {
                if (response.success) {
                    if (response.isAdded) {
                        icon.classList.remove("bi-heart");
                        icon.classList.add("bi-heart-fill", "text-danger");
                    } else {
                        icon.classList.remove("bi-heart-fill", "text-danger");
                        icon.classList.add("bi-heart");
                    }
                    return;
                }

                if (response.requireLogin) {
                    if (window.confirm(response.message)) {
                        window.location.href = loginUrl;
                    }
                    return;
                }

                window.alert(response.message);
            }).fail(function () {
                window.alert("Có lỗi xảy ra.");
            });
        });
    }

    function initZoomModal() {
        document.addEventListener("click", function (event) {
            var image = event.target.closest(".zoom-image");
            if (!image) {
                return;
            }

            showZoomModal(image.currentSrc || image.src, image.alt || "");
        });

        function showZoomModal(src, alt) {
            var modal = document.createElement("div");
            modal.className = "zoom-modal";
            modal.innerHTML = '<img src="' + src + '" alt="' + alt.replace(/"/g, "&quot;") + '" />';

            function closeModal() {
                if (document.body.contains(modal)) {
                    document.body.removeChild(modal);
                }
                document.removeEventListener("keydown", onEscape);
            }

            function onEscape(event) {
                if (event.key === "Escape") {
                    closeModal();
                }
            }

            modal.addEventListener("click", closeModal);
            document.addEventListener("keydown", onEscape);
            document.body.appendChild(modal);
        }
    }

    onReady(function () {
        initMobileNavigation();
        initDesktopShopLink();
        initSearchSuggestions();
        initQuickView();
        initProductDetail();
        initZoomModal();
    });
})();
