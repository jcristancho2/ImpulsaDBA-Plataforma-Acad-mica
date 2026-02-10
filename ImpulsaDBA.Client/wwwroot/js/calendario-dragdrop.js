// Drag and drop nativo para el calendario (evita que ondrop no se dispare en Blazor)
window.ImpulsaDBA_CalendarDragDrop = {
    _dotNetRef: null,
    _dragStartHandler: null,
    _dragOverHandler: null,
    _dropHandler: null,

    init: function (dotNetRef) {
        this._dotNetRef = dotNetRef;
        this._dragStartHandler = this._onDragStart.bind(this);
        this._dragOverHandler = this._onDragOver.bind(this);
        this._dropHandler = this._onDrop.bind(this);
        this._attach();
    },

    _attach: function () {
        document.querySelectorAll('[data-activity-id]').forEach(function (el) {
            if (el.getAttribute('data-activity-id')) el.addEventListener('dragstart', this._dragStartHandler);
        }.bind(this));
        document.querySelectorAll('[data-drop-day]').forEach(function (el) {
            el.addEventListener('dragover', this._dragOverHandler);
            el.addEventListener('drop', this._dropHandler);
        }.bind(this));
    },

    _detach: function () {
        document.querySelectorAll('[data-activity-id]').forEach(function (el) {
            el.removeEventListener('dragstart', this._dragStartHandler);
        }.bind(this));
        document.querySelectorAll('[data-drop-day]').forEach(function (el) {
            el.removeEventListener('dragover', this._dragOverHandler);
            el.removeEventListener('drop', this._dropHandler);
        }.bind(this));
    },

    _onDragStart: function (e) {
        var id = e.target.getAttribute('data-activity-id');
        if (id) {
            e.dataTransfer.setData('text/plain', id);
            e.dataTransfer.effectAllowed = 'move';
        }
    },

    _onDragOver: function (e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
    },

    _onDrop: function (e) {
        e.preventDefault();
        var id = e.dataTransfer.getData('text/plain');
        var fecha = e.currentTarget.getAttribute('data-fecha');
        if (id && fecha && this._dotNetRef) {
            this._dotNetRef.invokeMethodAsync('HandleCalendarDrop', parseInt(id, 10), fecha);
        }
    },

    update: function (dotNetRef) {
        this._detach();
        this._dotNetRef = dotNetRef;
        var self = this;
        setTimeout(function () { self._attach(); }, 0);
    },

    dispose: function () {
        this._detach();
        this._dotNetRef = null;
    }
};
