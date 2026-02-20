/* PrismJS 1.29.0 - Minimal stub for syntax highlighting
   In production, replace with the full Prism.js bundle from https://prismjs.com/download.html
   Languages: csharp, markup (razor/xml), bash, json */

var Prism = (function () {
    var lang = {
        markup: {
            pattern: /<\/?[\w.:-]+\s*(?:\s+[\w.:$-]+(?:=(?:"[^"]*"|'[^']*'|[^\s"'=<>`]+))?)*\s*\/?>/g,
            inside: {}
        }
    };

    function highlightAll() {
        var elements = document.querySelectorAll('code[class*="language-"], code[class*="lang-"]');
        for (var i = 0; i < elements.length; i++) {
            highlightElement(elements[i]);
        }
    }

    function highlightElement(element) {
        if (!element) return;
        // Add basic styling class
        var pre = element.parentElement;
        if (pre && pre.tagName === 'PRE') {
            pre.classList.add('language-highlighted');
        }
    }

    return {
        languages: lang,
        highlightAll: highlightAll,
        highlightElement: highlightElement,
        highlight: function (text) { return text; }
    };
})();

if (typeof module !== 'undefined') {
    module.exports = Prism;
}
