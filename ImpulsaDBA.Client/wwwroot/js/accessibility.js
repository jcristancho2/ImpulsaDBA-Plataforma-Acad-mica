/**
 * WCAG: Focus trap para modales (2.1.2 Sin atrapamiento del teclado).
 * Mantiene el foco dentro del modal con TAB y restaura el foco al cerrar.
 */
(function () {
    var savedPrevious = null;
    var savedModalEl = null;
    var savedListener = null;

    function getFocusable(el) {
        if (!el) return [];
        var selector = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';
        return [].slice.call(el.querySelectorAll(selector)).filter(function (node) {
            return node.offsetParent !== null && !node.hasAttribute('disabled');
        });
    }

    function handleTab(e, modalEl) {
        if (e.key !== 'Tab') return;
        var focusable = getFocusable(modalEl);
        if (focusable.length === 0) return;
        var current = document.activeElement;
        var idx = focusable.indexOf(current);
        if (e.shiftKey) {
            if (idx <= 0) {
                e.preventDefault();
                focusable[focusable.length - 1].focus();
            }
        } else {
            if (idx === focusable.length - 1 || idx === -1) {
                e.preventDefault();
                focusable[0].focus();
            }
        }
    }

    window.impulsaAcc = window.impulsaAcc || {};
    window.impulsaAcc.activateModalTrap = function (modalElementOrId) {
        var modalEl = typeof modalElementOrId === 'string' ? document.getElementById(modalElementOrId) : modalElementOrId;
        if (!modalEl) return;
        savedPrevious = document.activeElement;
        savedModalEl = modalEl;
        var focusable = getFocusable(modalElement);
        if (focusable.length > 0) {
            focusable[0].focus();
        }
        savedListener = function (e) {
            handleTab(e, modalEl);
        };
        modalEl.addEventListener('keydown', savedListener);
    };

    window.impulsaAcc.deactivateModalTrap = function () {
        if (savedModalEl && savedListener) {
            savedModalEl.removeEventListener('keydown', savedListener);
            savedModalEl = null;
            savedListener = null;
        }
        if (savedPrevious && typeof savedPrevious.focus === 'function') {
            try {
                savedPrevious.focus();
            } catch (err) {}
        }
        savedPrevious = null;
    };

    // Wrappers para invocación desde Blazor (JSInvoker requiere nombre global)
    window.impulsaAccActivateModalTrap = function (id) {
        window.impulsaAcc.activateModalTrap(id);
    };
    window.impulsaAccDeactivateModalTrap = function () {
        window.impulsaAcc.deactivateModalTrap();
    };
})();
