window.BlazorDocs = {
    _searchRef: null,

    highlightAll: function () {
        if (typeof Prism !== 'undefined') {
            Prism.highlightAll();
        }
    },

    highlightElement: function (element) {
        if (typeof Prism !== 'undefined' && element) {
            Prism.highlightElement(element);
        }
    },

    copyToClipboard: function (text) {
        if (navigator.clipboard) {
            return navigator.clipboard.writeText(text);
        }
    },

    registerSearchShortcut: function (dotNetRef) {
        this._searchRef = dotNetRef;
        this._keyHandler = function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('Open');
                }
            }
        };
        document.addEventListener('keydown', this._keyHandler);
    },

    unregisterSearchShortcut: function () {
        if (this._keyHandler) {
            document.removeEventListener('keydown', this._keyHandler);
            this._keyHandler = null;
        }
        this._searchRef = null;
    },

    scrollNavToActive: function () {
        var active = document.querySelector('.docs-sidebar .nav-link.active');
        if (active) {
            active.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    }
};
