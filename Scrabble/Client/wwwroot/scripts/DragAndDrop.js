// Function to adapt to browser layout:
//  Square game board on maximized desktop window should not create vertical scrollbar
//  Square game board on portrait mobile layout should extend to left and right margin
//     Landscape mobile not implemented
var initialDisplay = true;

// debug logging switches and associated functions
var wszDebugging = false;  // window sizing
var evtDebugging = false;  // event listeners/handlers
var evtInitialSetupLogging = false;  // control logging during initial setup
var dndDebugging = false;  // drag and drop operations

// flag to switch use of removeEventListener
// to make changing behaviour during testing/debugging easier
// after much testing and game playing it seems leaving it set to false
// is the best option
var rmEvtListener = false;

function wszLogMessage(msg) {
    if (wszDebugging)
        console.log(msg);
}

function evtLogMessage(msg) {
    if (evtDebugging)
        console.log(msg);
}

function dndLogMessage(msg) {
    if (dndDebugging)
        console.log(msg);
}


function handleWindowSize()
{
    var browserHeight = window.innerHeight;
    var browserWidth = window.innerWidth;
    var devicePixelRatio = window.devicePixelRatio;
    wszLogMessage(" ");
    wszLogMessage("---");
    wszLogMessage("Browser dimensions:")
    wszLogMessage("   width=" + browserWidth + ", height=" + browserHeight + ", pixelRatio=" + devicePixelRatio);

    var gameArea = document.getElementById('game');
    if (!gameArea)
    {
        return;
    }
    wszLogMessage("GAME AREA - offsetWidth=" + gameArea.offsetWidth + ", offsetHeight=" + gameArea.offsetHeight);

    // can also use getBoundingClientRect() on an element but note it returns non-integer values
    // so just use offsetWidth and offsetHeight
    var gameBoard = document.getElementById('game-board');
    var gameBoardColumnWidth = gameBoard.offsetWidth;

    var sidebarColumn = document.getElementById('sidebar');
    var sidebarColumnWidth = sidebarColumn.offsetWidth;

    var totalScoresColumn = document.getElementById('total-scores');
    var totalScoresColumnWidth = totalScoresColumn.offsetWidth;

    var recentMovesColumn = document.getElementById('recent-moves');
    var recentMovesColumnWidth = recentMovesColumn.offsetWidth;

    wszLogMessage("GB - offsetWidth=" + gameBoard.offsetWidth + ", offsetHeight=" + gameBoard.offsetHeight);
    //wszLogMessage("SB - clientWidth=" + sidebarColumn.clientWidth + ", clientHeight=" + sidebarColumn.clientHeight);
    wszLogMessage("SB - offsetWidth=" + sidebarColumn.offsetWidth + ", offsetHeight=" + sidebarColumn.offsetHeight);
    wszLogMessage("TS - offsetWidth=" + totalScoresColumn.offsetWidth + ", offsetHeight=" + totalScoresColumn.offsetHeight);
    wszLogMessage("RM - offsetWidth=" + recentMovesColumn.offsetWidth + ", offsetHeight=" + recentMovesColumn.offsetHeight);

    var gamePlayRow = document.getElementById('game-play-row');
    var gamePlayRowHeight = gamePlayRow.offsetHeight;
    var actionButtonsRow = document.getElementById('action-buttons-row');
    var actionButtonsRowHeight = actionButtonsRow.offsetHeight;
    wszLogMessage("GP/AB - gamePlayRowHeight=" + gamePlayRowHeight + ", actionButtonsRowHeight=" + actionButtonsRowHeight);

    var isLandscape = false;
    if (browserWidth >= browserHeight)
    {
        isLandscape = true;
        wszLogMessage("~ Landscape mode detected ~");
    }
    else
    {
        wszLogMessage("~ Portrait mode detected ~");
    }

    /*
    var newHeight = browserHeight;
    var newWidth = browserWidth;
    if (newWidth <= (sidebarColumn.clientWidth + 50)) {
        // Sidebar is top-bar so adjust the height
        newHeight -= sidebarColumn.offsetHeight;
    } else {
        // Sidebar is side-bar so adjust the width
        newWidth -= sidebarColumn.offsetWidth;
    }
    */

    /* size the board */
    var boardSize = 0;

    var newHeight = browserHeight;
    var adjHeight = 2 * (gamePlayRowHeight + actionButtonsRowHeight);
    newHeight -= adjHeight;

    var newWidth = browserWidth;
    var requiredWidth = sidebarColumnWidth + totalScoresColumnWidth + gameBoardColumnWidth + recentMovesColumnWidth;
    var adjWidth = sidebarColumnWidth + totalScoresColumnWidth + recentMovesColumnWidth;
    if (requiredWidth > browserWidth)
    {
        adjWidth = sidebarColumnWidth + totalScoresColumnWidth;
    }

    newWidth -= adjWidth;
    boardSize = Math.min(newWidth, newHeight);

    wszLogMessage("*** new width=" + newWidth + ", new height=" + newHeight + ", board size=" + boardSize);

    // numbers in px units...
    const MIN_SQUARE_SIZE = 32;
    // this is the padding between each row and column - it doesn't allow for padding
    // to the left of the leftmost cell or to the right of the rightmost
    const squarePadding = 2;
    // here we account for unaccounted for border on the left and right hand cells and add
    // an extra 2 pixels for a complete 2 pixel wide border around the whole grid
    const gridBorderPadding = squarePadding + 2;
    // and then give the whole thing a 3px wide red border...
    const gridBorderWidth = 3;
    // and it all adds up to...
    const PADDING_AND_WIDTH = 14 * squarePadding + 2 * gridBorderPadding + 2 * gridBorderWidth;
    // giving a minimum value for the board size of...
    var minBoardSize = 15 * MIN_SQUARE_SIZE + PADDING_AND_WIDTH;
    if (boardSize < minBoardSize && !initialDisplay)
    {
        // Don't resize if the game area is too small
        console.log("!!! 'board size' value is too small (min. " + minBoardSize + ") - NOT resizing !!!");
        return;
    }

    initialDisplay = false;

    var squarePaddingPx = squarePadding + "px";
    var gridBorderPaddingPx = gridBorderPadding + "px";
    //for testing - gridBorderPaddingPx = "0px";
    var gridBorderWidthPx = gridBorderWidth + "px";

    document.documentElement.style.setProperty('--row-column-gap', squarePaddingPx);
    document.documentElement.style.setProperty('--grid-border-padding', gridBorderPaddingPx);
    document.documentElement.style.setProperty('--grid-border-width', gridBorderWidthPx);

    // the size of the main board tile in px
    var squareSize = Math.floor((boardSize - PADDING_AND_WIDTH) / 15);

    // don't let tiles get too small
    if (squareSize < MIN_SQUARE_SIZE) squareSize = MIN_SQUARE_SIZE;

    // re-compute board size now that squareSize has been decided
    boardSize = 15 * squareSize + PADDING_AND_WIDTH;
    console.log("Adjusted board size=" + boardSize);

    gameArea.style.height = boardSize + "px";
    gameArea.style.width = boardSize + "px";

    // the size of the rack tile in px (30% bigger)
    var rackSize = Math.floor(1.3 * squareSize);
    // but the font size 20% bigger
    var rackFontSize = Math.floor(1.2 * squareSize);

    var squareSizePx = squareSize + "px";
    var rackSizePx = rackSize + "px";

    // the next two relate to the special multiplier squares - DL,DW,TL,TW
    // they are the basic square size + an extra contribution to account for the "zigzag" annotation
    // that indicates a double or triple multiplier
    var multiplierSquareOffset = 2 * (squarePadding + 1);
    var multiplierSquareSizePx = (squareSize + 2 * multiplierSquareOffset) + "px";
    // this is an offset to position the "oversized" special multiplier squares correctly on the board
    var multiplierSquareOffsetPx = (-1 * multiplierSquareOffset) + "px";

    wszLogMessage("squareSizePx=" + squareSizePx + ", rackSizePx=" + rackSizePx);
    wszLogMessage("multiplierSquareSizePx=" + multiplierSquareSizePx + ", multiplierSquareOffsetPx=" + multiplierSquareOffsetPx);

    document.documentElement.style.setProperty('--square-size', squareSizePx);
    document.documentElement.style.setProperty('--tile-rack-size', rackSizePx);

    document.documentElement.style.setProperty('--multiplier-square-size', multiplierSquareSizePx);
    document.documentElement.style.setProperty('--multiplier-square-offset', multiplierSquareOffsetPx);

    // font sizes for a board letter and its value...
    var boardLetterFs = Math.floor(0.7 * squareSize);
    var boardLetterValueFs = Math.floor(0.35 * squareSize);
    // the 'annotation' font size is for the text in the DL, DW, TL, TW squares
    var boardSquareAnnotationFs = Math.floor(0.5 * squareSize);
    document.documentElement.style.setProperty('--board-letter-fs', boardLetterFs + "px");
    document.documentElement.style.setProperty('--board-letter-value-fs', boardLetterValueFs + "px");
    document.documentElement.style.setProperty('--board-square-annotation-fs', boardSquareAnnotationFs + "px");
    wszLogMessage("board-letter-fs=" + boardLetterFs + ", board-letter-value-fs=" + boardLetterValueFs + ", board-square-annotation-fs=" + boardSquareAnnotationFs);

    // font sizes for a rack letter and its value...
    // ...so that everything is in better proportion when displayed on a tile in the rack
    var rackLetterFs = Math.floor(0.7 * rackFontSize);
    var rackLetterValueFs = Math.floor(0.35 * rackFontSize);
    document.documentElement.style.setProperty('--rack-letter-fs', rackLetterFs + "px");
    document.documentElement.style.setProperty('--rack-letter-value-fs', rackLetterValueFs + "px");
    wszLogMessage("rack-letter-fs=" + rackLetterFs + ", rack-letter-value-fs=" + rackLetterValueFs);
    wszLogMessage("---");
    wszLogMessage(" ");
}


/* Javascript functions to assist with HTML drag/drop operations.
 * Generic Blazor drag / drop Interop libraries such as Plk.Blazor.DragDrop work great
 * for a limited number of items, but bog down with 225 Scrabble squares.
 * */


/* ---- A series of functions to convert touch events to HTML5 drag/drop events ----------------- 
   From http://bernardo-castilho.github.io/DragDropTouch/DragDropTouch.js
 */
var DragDropTouch;
(function (DragDropTouch_1) {
    'use strict';
    /**
     * Object used to hold the data that is being dragged during drag and drop operations.
     *
     * It may hold one or more data items of different types. For more information about
     * drag and drop operations and data transfer objects, see
     * <a href="https://developer.mozilla.org/en-US/docs/Web/API/DataTransfer">HTML Drag and Drop API</a>.
     *
     * This object is created automatically by the @see:DragDropTouch singleton and is
     * accessible through the @see:dataTransfer property of all drag events.
     */
    var DataTransfer = (function () {
        function DataTransfer() {
            this._dropEffect = 'move';
            this._effectAllowed = 'all';
            this._data = {};
        }
        Object.defineProperty(DataTransfer.prototype, "dropEffect", {
            /**
             * Gets or sets the type of drag-and-drop operation currently selected.
             * The value must be 'none',  'copy',  'link', or 'move'.
             */
            get: function () {
                return this._dropEffect;
            },
            set: function (value) {
                this._dropEffect = value;
            },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(DataTransfer.prototype, "effectAllowed", {
            /**
             * Gets or sets the types of operations that are possible.
             * Must be one of 'none', 'copy', 'copyLink', 'copyMove', 'link',
             * 'linkMove', 'move', 'all' or 'uninitialized'.
             */
            get: function () {
                return this._effectAllowed;
            },
            set: function (value) {
                this._effectAllowed = value;
            },
            enumerable: true,
            configurable: true
        });
        Object.defineProperty(DataTransfer.prototype, "types", {
            /**
             * Gets an array of strings giving the formats that were set in the @see:dragstart event.
             */
            get: function () {
                return Object.keys(this._data);
            },
            enumerable: true,
            configurable: true
        });
        /**
         * Removes the data associated with a given type.
         *
         * The type argument is optional. If the type is empty or not specified, the data
         * associated with all types is removed. If data for the specified type does not exist,
         * or the data transfer contains no data, this method will have no effect.
         *
         * @param type Type of data to remove.
         */
        DataTransfer.prototype.clearData = function (type) {
            if (type != null) {
                delete this._data[type.toLowerCase()];
            }
            else {
                this._data = {};
            }
        };
        /**
         * Retrieves the data for a given type, or an empty string if data for that type does
         * not exist or the data transfer contains no data.
         *
         * @param type Type of data to retrieve.
         */
        DataTransfer.prototype.getData = function (type) {
            return this._data[type.toLowerCase()] || '';
        };
        /**
         * Set the data for a given type.
         *
         * For a list of recommended drag types, please see
         * https://developer.mozilla.org/en-US/docs/Web/Guide/HTML/Recommended_Drag_Types.
         *
         * @param type Type of data to add.
         * @param value Data to add.
         */
        DataTransfer.prototype.setData = function (type, value) {
            this._data[type.toLowerCase()] = value;
        };
        /**
         * Set the image to be used for dragging if a custom one is desired.
         *
         * @param img An image element to use as the drag feedback image.
         * @param offsetX The horizontal offset within the image.
         * @param offsetY The vertical offset within the image.
         */
        DataTransfer.prototype.setDragImage = function (img, offsetX, offsetY) {
            var ddt = DragDropTouch._instance;
            ddt._imgCustom = img;
            ddt._imgOffset = { x: offsetX, y: offsetY };
        };
        return DataTransfer;
    }());
    DragDropTouch_1.DataTransfer = DataTransfer;
    /**
     * Defines a class that adds support for touch-based HTML5 drag/drop operations.
     *
     * The @see:DragDropTouch class listens to touch events and raises the
     * appropriate HTML5 drag/drop events as if the events had been caused
     * by mouse actions.
     *
     * The purpose of this class is to enable using existing, standard HTML5
     * drag/drop code on mobile devices running IOS or Android.
     *
     * To use, include the DragDropTouch.js file on the page. The class will
     * automatically start monitoring touch events and will raise the HTML5
     * drag drop events (dragstart, dragenter, dragleave, drop, dragend) which
     * should be handled by the application.
     *
     * For details and examples on HTML drag and drop, see
     * https://developer.mozilla.org/en-US/docs/Web/Guide/HTML/Drag_operations.
     */
    var DragDropTouch = (function () {
        /**
         * Initializes the single instance of the @see:DragDropTouch class.
         */
        function DragDropTouch() {
            this._lastClick = 0;
            // enforce singleton pattern
            if (DragDropTouch._instance) {
                throw 'DragDropTouch instance already created.';
            }
            // detect passive event support
            // https://github.com/Modernizr/Modernizr/issues/1894
            var supportsPassive = false;
            document.addEventListener('test', function () { }, {
                get passive() {
                    supportsPassive = true;
                    return true;
                }
            });
            // listen to touch events
            if (navigator.maxTouchPoints) {
                var d = document,
                    ts = this._touchstart.bind(this),
                    tm = this._touchmove.bind(this),
                    te = this._touchend.bind(this),
                    opt = supportsPassive ? { passive: false, capture: false } : false;
                d.addEventListener('touchstart', ts, opt);
                d.addEventListener('touchmove', tm, opt);
                d.addEventListener('touchend', te);
                d.addEventListener('touchcancel', te);
            }
        }
        /**
         * Gets a reference to the @see:DragDropTouch singleton.
         */
        DragDropTouch.getInstance = function () {
            return DragDropTouch._instance;
        };
        // ** event handlers
        DragDropTouch.prototype._touchstart = function (e) {
            var _this = this;
            if (this._shouldHandle(e)) {
                // raise double-click and prevent zooming
                if (Date.now() - this._lastClick < DragDropTouch._DBLCLICK) {
                    if (this._dispatchEvent(e, 'dblclick', e.target)) {
                        e.preventDefault();
                        this._reset();
                        return;
                    }
                }
                // clear all variables
                this._reset();
                // get nearest draggable element
                var src = this._closestDraggable(e.target);
                if (src) {
                    // give caller a chance to handle the hover/move events
                    if (!this._dispatchEvent(e, 'mousemove', e.target) &&
                        !this._dispatchEvent(e, 'mousedown', e.target)) {
                        // get ready to start dragging
                        this._dragSource = src;
                        this._ptDown = this._getPoint(e);
                        this._lastTouch = e;
                        e.preventDefault();
                        // show context menu if the user hasn't started dragging after a while
                        setTimeout(function () {
                            if (_this._dragSource == src && _this._img == null) {
                                if (_this._dispatchEvent(e, 'contextmenu', src)) {
                                    _this._reset();
                                }
                            }
                        }, DragDropTouch._CTXMENU);
                        if (DragDropTouch._ISPRESSHOLDMODE) {
                            this._pressHoldInterval = setTimeout(function () {
                                _this._isDragEnabled = true;
                                _this._touchmove(e);
                            }, DragDropTouch._PRESSHOLDAWAIT);
                        }
                    }
                }
            }
        };
        DragDropTouch.prototype._touchmove = function (e) {
            if (this._shouldCancelPressHoldMove(e)) {
                this._reset();
                return;
            }
            if (this._shouldHandleMove(e) || this._shouldHandlePressHoldMove(e)) {
                // see if target wants to handle move
                var target = this._getTarget(e);
                if (this._dispatchEvent(e, 'mousemove', target)) {
                    this._lastTouch = e;
                    e.preventDefault();
                    return;
                }
                // start dragging
                if (this._dragSource && !this._img && this._shouldStartDragging(e)) {
                    this._dispatchEvent(e, 'dragstart', this._dragSource);
                    this._createImage(e);
                    this._dispatchEvent(e, 'dragenter', target);
                }
                // continue dragging
                if (this._img) {
                    this._lastTouch = e;
                    e.preventDefault(); // prevent scrolling
                    this._dispatchEvent(e, 'drag', this._dragSource);
                    if (target != this._lastTarget) {
                        this._dispatchEvent(this._lastTouch, 'dragleave', this._lastTarget);
                        this._dispatchEvent(e, 'dragenter', target);
                        this._lastTarget = target;
                    }
                    this._moveImage(e);
                    this._isDropZone = this._dispatchEvent(e, 'dragover', target);
                }
            }
        };
        DragDropTouch.prototype._touchend = function (e) {
            if (this._shouldHandle(e)) {
                // see if target wants to handle up
                if (this._dispatchEvent(this._lastTouch, 'mouseup', e.target)) {
                    e.preventDefault();
                    return;
                }
                // user clicked the element but didn't drag, so clear the source and simulate a click
                if (!this._img) {
                    this._dragSource = null;
                    this._dispatchEvent(this._lastTouch, 'click', e.target);
                    this._lastClick = Date.now();
                }
                // finish dragging
                this._destroyImage();
                if (this._dragSource) {
                    if (e.type.indexOf('cancel') < 0 && this._isDropZone) {
                        this._dispatchEvent(this._lastTouch, 'drop', this._lastTarget);
                    }
                    this._dispatchEvent(this._lastTouch, 'dragend', this._dragSource);
                    this._reset();
                }
            }
        };
        // ** utilities
        // ignore events that have been handled or that involve more than one touch
        DragDropTouch.prototype._shouldHandle = function (e) {
            return e &&
                !e.defaultPrevented &&
                e.touches && e.touches.length < 2;
        };

        // use regular condition outside of press & hold mode
        DragDropTouch.prototype._shouldHandleMove = function (e) {
            return !DragDropTouch._ISPRESSHOLDMODE && this._shouldHandle(e);
        };

        // allow to handle moves that involve many touches for press & hold
        DragDropTouch.prototype._shouldHandlePressHoldMove = function (e) {
            return DragDropTouch._ISPRESSHOLDMODE &&
                this._isDragEnabled && e && e.touches && e.touches.length;
        };

        // reset data if user drags without pressing & holding
        DragDropTouch.prototype._shouldCancelPressHoldMove = function (e) {
            return DragDropTouch._ISPRESSHOLDMODE && !this._isDragEnabled &&
                this._getDelta(e) > DragDropTouch._PRESSHOLDMARGIN;
        };

        // start dragging when specified delta is detected
        DragDropTouch.prototype._shouldStartDragging = function (e) {
            var delta = this._getDelta(e);
            return delta > DragDropTouch._THRESHOLD ||
                (DragDropTouch._ISPRESSHOLDMODE && delta >= DragDropTouch._PRESSHOLDTHRESHOLD);
        }

        // clear all members
        DragDropTouch.prototype._reset = function () {
            this._destroyImage();
            this._dragSource = null;
            this._lastTouch = null;
            this._lastTarget = null;
            this._ptDown = null;
            this._isDragEnabled = false;
            this._isDropZone = false;
            this._dataTransfer = new DataTransfer();
            clearInterval(this._pressHoldInterval);
        };
        // get point for a touch event
        DragDropTouch.prototype._getPoint = function (e, page) {
            if (e && e.touches) {
                e = e.touches[0];
            }
            return { x: page ? e.pageX : e.clientX, y: page ? e.pageY : e.clientY };
        };
        // get distance between the current touch event and the first one
        DragDropTouch.prototype._getDelta = function (e) {
            if (DragDropTouch._ISPRESSHOLDMODE && !this._ptDown) { return 0; }
            var p = this._getPoint(e);
            return Math.abs(p.x - this._ptDown.x) + Math.abs(p.y - this._ptDown.y);
        };
        // get the element at a given touch event
        DragDropTouch.prototype._getTarget = function (e) {
            var pt = this._getPoint(e), el = document.elementFromPoint(pt.x, pt.y);
            while (el && getComputedStyle(el).pointerEvents == 'none') {
                el = el.parentElement;
            }
            return el;
        };
        // create drag image from source element
        DragDropTouch.prototype._createImage = function (e) {
            // just in case...
            if (this._img) {
                this._destroyImage();
            }
            // create drag image from custom element or drag source
            var src = this._imgCustom || this._dragSource;
            this._img = src.cloneNode(true);
            this._copyStyle(src, this._img);
            this._img.style.top = this._img.style.left = '-9999px';
            // if creating from drag source, apply offset and opacity
            if (!this._imgCustom) {
                var rc = src.getBoundingClientRect(), pt = this._getPoint(e);
                this._imgOffset = { x: pt.x - rc.left, y: pt.y - rc.top };
                this._img.style.opacity = DragDropTouch._OPACITY.toString();
            }
            // add image to document
            this._moveImage(e);
            document.body.appendChild(this._img);
        };
        // dispose of drag image element
        DragDropTouch.prototype._destroyImage = function () {
            if (this._img && this._img.parentElement) {
                this._img.parentElement.removeChild(this._img);
            }
            this._img = null;
            this._imgCustom = null;
        };
        // move the drag image element
        DragDropTouch.prototype._moveImage = function (e) {
            var _this = this;
            requestAnimationFrame(function () {
                if (_this._img) {
                    var pt = _this._getPoint(e, true), s = _this._img.style;
                    s.position = 'absolute';
                    s.pointerEvents = 'none';
                    s.zIndex = '999999';
                    s.left = Math.round(pt.x - _this._imgOffset.x) + 'px';
                    s.top = Math.round(pt.y - _this._imgOffset.y) + 'px';
                }
            });
        };
        // copy properties from an object to another
        DragDropTouch.prototype._copyProps = function (dst, src, props) {
            for (var i = 0; i < props.length; i++) {
                var p = props[i];
                dst[p] = src[p];
            }
        };
        DragDropTouch.prototype._copyStyle = function (src, dst) {
            // remove potentially troublesome attributes
            DragDropTouch._rmvAtts.forEach(function (att) {
                dst.removeAttribute(att);
            });
            // copy canvas content
            if (src instanceof HTMLCanvasElement) {
                var cSrc = src, cDst = dst;
                cDst.width = cSrc.width;
                cDst.height = cSrc.height;
                cDst.getContext('2d').drawImage(cSrc, 0, 0);
            }
            // copy style (without transitions)
            var cs = getComputedStyle(src);
            for (var i = 0; i < cs.length; i++) {
                var key = cs[i];
                if (key.indexOf('transition') < 0) {
                    dst.style[key] = cs[key];
                }
            }
            dst.style.pointerEvents = 'none';
            // and repeat for all children
            for (var i = 0; i < src.children.length; i++) {
                this._copyStyle(src.children[i], dst.children[i]);
            }
        };
        DragDropTouch.prototype._dispatchEvent = function (e, type, target) {
            if (e && target) {
                var evt = document.createEvent('Event'), t = e.touches ? e.touches[0] : e;
                evt.initEvent(type, true, true);
                evt.button = 0;
                evt.which = evt.buttons = 1;
                this._copyProps(evt, e, DragDropTouch._kbdProps);
                this._copyProps(evt, t, DragDropTouch._ptProps);
                evt.dataTransfer = this._dataTransfer;
                target.dispatchEvent(evt);
                return evt.defaultPrevented;
            }
            return false;
        };
        // gets an element's closest draggable ancestor
        DragDropTouch.prototype._closestDraggable = function (e) {
            for (; e; e = e.parentElement) {
                if (e.hasAttribute('draggable') && e.draggable) {
                    return e;
                }
            }
            return null;
        };
        return DragDropTouch;
    }());
    /*private*/ DragDropTouch._instance = new DragDropTouch(); // singleton
    // constants
    DragDropTouch._THRESHOLD = 5; // pixels to move before drag starts
    DragDropTouch._OPACITY = 0.5; // drag image opacity
    DragDropTouch._DBLCLICK = 500; // max ms between clicks in a double click
    DragDropTouch._CTXMENU = 900; // ms to hold before raising 'contextmenu' event
    DragDropTouch._ISPRESSHOLDMODE = false; // decides of press & hold mode presence
    DragDropTouch._PRESSHOLDAWAIT = 400; // ms to wait before press & hold is detected
    DragDropTouch._PRESSHOLDMARGIN = 25; // pixels that finger might shiver while pressing
    DragDropTouch._PRESSHOLDTHRESHOLD = 0; // pixels to move before drag starts
    // copy styles/attributes from drag source to drag image element
    DragDropTouch._rmvAtts = 'id,class,style,draggable'.split(',');
    // synthesize and dispatch an event
    // returns true if the event has been handled (e.preventDefault == true)
    DragDropTouch._kbdProps = 'altKey,ctrlKey,metaKey,shiftKey'.split(',');
    DragDropTouch._ptProps = 'pageX,pageY,clientX,clientY,screenX,screenY,offsetX,offsetY'.split(',');
    DragDropTouch_1.DragDropTouch = DragDropTouch;
})(DragDropTouch || (DragDropTouch = {}));

/* End of touch -> drag/drop ------------------------ */




function getTileLetter(parent) {
    if (parent.hasChildNodes()) {
        return "[" + parent.firstChild.textContent + "]";
    }
}

// previously if you dragged a tile that is placed on the board you would get 
// a dragstart event for the Tile followed by a dragstart event for the Square
// so there was code in place to block the events related to a Square and allow
// the Tile event to proceed
// but subsequently you might get one dragend event or sometimes two...
// to address this dragstart listener for "Square" has been removed (no reason to drag a Square)
// in addition the board is managed in such a way that Squares are deregistered as
// drop sites once they are occupied during game play (and at start up if the board
// is partly complete e.g. if a game is reloaded the event handlers are not registerd at all)

function handleDragStart(e) {
    const draggedId = this.getAttribute('id');

    dndLogMessage(" ");
    dndLogMessage("< < <");
    dndLogMessage("handleDragStart for '" + draggedId + "' " + getTileLetter(this));

    this.style.opacity = '0.4';

    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', draggedId);
}

function handleDragEnd(e) {
    const draggedId = this.getAttribute('id');

    dndLogMessage("handleDragEnd for '" + draggedId + "' " + getTileLetter(this));

    this.style.opacity = '1';

    dndLogMessage("> > >");
    dndLogMessage(" ");
}

function handleDragOver(e) {
    e.preventDefault();  // Drop will be supported
}

//export function handleDragEnter(e) {
//    //this.classList.add('over');
//}

//export function handleDragLeave(e) {
//    //this.classList.remove('over');
//}

export async function handleDrop(e) {
    e.preventDefault();

    const dragId = e.dataTransfer.getData('text/plain');
    var dropId = this.getAttribute('id');

    //console.log(e);
    dndLogMessage("handleDrop for : dragId(source) '" + dragId + "' -> dropId(target) '" + dropId + "'");

    // if we don't stop event propagation there are four scenarios and their associated events involving the board.
    // by allowing propagation we allow events to bubble. if we stop it then the "dragend" event is never delivered
    // to the registered handler. so, it's probably better to allow all the events to flow and to intercept and
    // suppress the second tile to square drop in scenarios 2 and 4
    // 1.rack tile -> empty square
    //  events (1) : drag Tile -> drop Square
    // 2.rack tile -> square with tile
    //  events (2) : drag Tile "A" -> drop Tile "B", drag Tile "A" -> drop Square
    // 3.square with tile -> empty square
    //  events (1) : drag Tile -> drop Square
    // 4.square with tile -> square with tile
    //  events (2) : drag Tile "A" -> drop Tile "B", drag Tile "A" -> drop Square
    //
    // There is a fifth scenario which involves only tiles on the rack when re-organising the order
    // 5.rack tile -> rack tile
    // events (1) : drag Tile "A" -> drop Tile "B"

    if (dropId.startsWith('Square')) {
        var hasTile = this.getElementsByClassName('tile-container');
        if (hasTile.length > 0) {  // the square contains a tile
            // one option is to simply ignore this event
            if (!rmEvtListener)
                return;

            // or
            // reference the tile as target and pass it to dotnet
            // but note that the tile id will be the tile that has been newly dragged on
            // to the square. so by replacing the dropId with the id of the tile now in
            // place, if we pass it on to DotNet it looks like a Tile -> same Tile drop ie 
            // a tile that hasn't moved from its original location
            dropId = hasTile[0].getAttribute('id');
            dndLogMessage("... now handleDrop for : dragId(source) '" + dragId + "' -> dropId(target) '" + dropId + "'");
        }
    }

    //e.stopPropagation();

    //if (!dragId || !dropId)
    //{
    //    console.log("Missing one or both of dragId / dropId");
    //    return;
    //}

    // let dotnet handle everthing, which allows for the tiles to have event handlers re-instated via
    // calls to SetTileForDrop() method
    await DotNet.invokeMethodAsync("Scrabble.Client", "HandleDropAsync", dragId, dropId);
}

function handleContextMenu(event) {
        event.preventDefault();
        event.stopPropagation();
}

// Allow focus set to element
export function SetFocusToElement(element) {
    element.focus();
};


// Disable context menu for tiles by interception
//document.getElementById('yourElement').oncontextmenu = function (event) {
//    event.preventDefault();
//    event.stopPropagation(); // not necessary in my case, could leave in case stopImmediateProp isn't available?
//    event.stopImmediatePropagation();
//    return false;
//};

function OnContextMenu(e) {
    event.preventDefault();
    event.stopPropagation(); // not necessary in my case, could leave in case stopImmediateProp isn't available? 
    event.stopImmediatePropagation();
    return false;
};

export function RmEventListenerForIdList(idList) {
    // idList passed as a semicolon delimited list of SquareId and TileId values
    // i.e. Square,1,1;Square,2,1;Tile,39; ...
    var ids = idList.split(";");
    for (var i = 0; i < ids.length; i++) {
        //evtLogMessage("(JS) RmEventListener for '" + ids[i] + "'")
        if (ids[i].startsWith("Square")) {
            let sq = document.getElementById(ids[i]);
            MyRemoveEventListenerByIdAndType(sq, 'dragover');
            MyRemoveEventListenerByIdAndType(sq, 'drop');
        }
        else if (ids[i].startsWith("Tile")) {
            let tl = document.getElementById(ids[i]);
            MyRemoveEventListenerByIdAndType(tl, 'dragstart');
            MyRemoveEventListenerByIdAndType(tl, 'dragover');
            MyRemoveEventListenerByIdAndType(tl, 'dragend');
            MyRemoveEventListenerByIdAndType(tl, 'drop');
            // context on tile for mobile
            MyRemoveEventListenerByIdAndType(tl, 'contextmenu');
        }
    }
}

export function SetEventListeners() {
    // this function is called once either for a new game where the board is blank or when
    // setting up an existing partially played game.
    // for the former, all 225 squares will be set up with dragover and drop event handlers
    // for the latter, only unoccupied squares will be set up with dragover and drop event handlers
    // unlike previously, the tiles on the tile rack will NOT have event handlers set up as this
    // is taken care of by the C# code via a call to the JS routine SetTileForDrop()

    // override debug logging for startup - hopefully nothing else will call evtLogMessage()
    // while SetEventListeners() is executing and before the evtDebugging flag can be re-instated
    var _evtDebugging = evtDebugging;
    evtDebugging = evtInitialSetupLogging;

    var oc = 0;
    var noc = 0;
    let sqs = document.querySelectorAll('.square');
    sqs.forEach(function (sq) {
        let tls = sq.querySelectorAll('.tile-container');
        if (tls.length > 0) {
            oc++;
        }
        else {
            noc++;
            MyAddEventListener(sq, 'dragover', handleDragOver);
            MyAddEventListener(sq, 'drop', handleDrop);
        }
    });

    console.log(oc + " occupied");
    console.log(noc + " not occupied");

    // re-instate the debug logging flag
    evtDebugging = _evtDebugging;
}

function showTileTextContent(parent, msg) {
    var details = "";
    if (parent.hasChildNodes()) {
        /*let children = parent.childNodes;
        for (const node of children) {
            details = details + node.textContent + " ";
        }*/
        details = parent.firstChild.textContent;
    }
    console.log(msg + " [" + details + "]");
}

export function SetTileForDrop(tileId) {
    evtLogMessage("(JS) SetTileForDrop '" + tileId + "'");
    let tile = document.getElementById(tileId);
    if (!tile) {
        console.log("(JS) SetTileForDrop : Unable to find tileId '" + tileId + "' in DOM");
        return; // Logic error
    }
    //showTileTextContent(tile, "(JS) Enable DnD for tileId '" + tileId + "'");

    MyAddEventListener(tile, 'dragstart', handleDragStart);
    MyAddEventListener(tile, 'dragover', handleDragOver);
    MyAddEventListener(tile, 'dragend', handleDragEnd);
    MyAddEventListener(tile, 'drop', handleDrop);

    // Prevent context on tile for mobile
    MyAddEventListener(tile, 'contextmenu', handleContextMenu, true);

    //MyAddEventListener(tile, 'contextmenu', function (event) {
    //    event.preventDefault();
    //    event.stopPropagation();
    //}, true);
}

export async function InitializeDragAndDrop() {
    // do this in case there's anything 'hanging around'
    // but there really shouldn't be
    MyRemoveAllEventListeners();

    // Delayed initialize to ensure browser rendering complete
    setTimeout(() => {
        SetEventListeners(); // For board squares
        handleWindowSize();  // Auto size game board to window/browser layout
        window.onresize = handleWindowSize;
    }, 1000);
}


async function createBlobFromURL(url) {
    // Fetch the contents of the file from the URL
    const response = await fetch(url);

    if (!response.ok) {
        throw new Error('Network response was not ok ' + response.statusText);
    }

    // Get the file content as an ArrayBuffer
    const arrayBuffer = await response.arrayBuffer();

    // Create a Blob from the ArrayBuffer
    const blob = new Blob([arrayBuffer], { type: response.headers.get('Content-Type') });

    return blob;
}

// Play the sound specified in the filename URL path
export function playSound(audioFilename) {
    // Example usage:
    //https://www.scrapplegame.us/sounds/Negative2.mp3
    createBlobFromURL(audioFilename)
        .then(blob => {
            console.log('Audio blob created:', blob);
            const blobURL = window.URL.createObjectURL(blob);
            var audio0 = new Audio(blobURL);
            audio0.play();

        })
        .catch(error => {
            console.error('Error creating audio Blob:', error);
        });

}



// Track event listeners
var my_listeners = {};
class MyHandler {
    // Each handler has four properties. the object itself, its attribute e.g. Tile,41
    // the handler function and any options
    // the "primary key" is the attr_id so for each type of event there should only be
    // one handler registered for it at any time
    constructor(obj_id, attr_id, fn, options){
        this.obj_id = obj_id
        this.attr_id = attr_id
        this.fn = fn
        this.options = options
    }
}
function MyAddEventListener(obj_id, type, fn, options = null)
{
    var attr_id = obj_id.getAttribute("id");
    var letter = "";
    if (attr_id.startsWith("Tile"))
        letter = getTileLetter(obj_id);

    if (!my_listeners[type])
        my_listeners[type] = [];

    var index = -1;
    for (let i = 0; i < my_listeners[type].length; i++)
    {
        if (my_listeners[type][i].attr_id == attr_id)
        {
            index = i;
            break;
        }
    }

    var handler = new MyHandler(obj_id, attr_id, fn, options);
    var action = "Add";
    if (index == -1)
        my_listeners[type].push(handler);
    else
    {
        // just checking !
        //console.log("== " + (obj_id == my_listeners[type][index].obj_id));
        //console.log("=== " + (obj_id === my_listeners[type][index].obj_id));

        action = "Update";
        // it seemed like removing the old handler from the object before registering a new one would be a good
        // idea. however, it seems to cause issues with some DnD scenarios which are difficult to predict ie
        // it doesn't happen every time a DnD action is being performed
        // feels like there is an issue being obscured here with timing of callbacks or due to a lack of understanding
        // of how the events and actions are processed etc which will bite on occasions?
        // i dont really understand why removing the event listener breaks things...
        // other than perhaps preventing in-flight events from being delivered and GC kicking in to
        // remove objects - since the characteristics of the "fail" scenarios involve missing id values eg 
        // instead of "Tile,42" we get "" or undefined because, on inspecting html for Tile,42 none of the events
        // that should have been added are present - so it's almost like they are added and then removed
        // rather than being removed and then added
        // for now it seems safer to set rmEvtListener to false and not try to remove the listener
        if (rmEvtListener)
            my_listeners[type][index].obj_id.removeEventListener(type, my_listeners[type][index].fn, my_listeners[type][index].options);

        // update our list for the object with the new handler
        my_listeners[type][index] = handler;
    }

    // register/re-register the handler
    obj_id.addEventListener(type, fn, options);
    evtLogMessage(action + " handler for '" + attr_id + letter + "' [" + type + "/" + my_listeners[type].length +"]");
}
function MyRemoveAllEventListeners()
{
    evtLogMessage("(JS) RemoveAllEventListeners");

    MyRemoveEventListenerByType('dragstart');
    MyRemoveEventListenerByType('dragover');
    MyRemoveEventListenerByType('dragend');
    MyRemoveEventListenerByType('drop');
    MyRemoveEventListenerByType('contextmenu');

    my_listeners = {};
}
function MyRemoveEventListenerByType(type)
{
    if (!my_listeners[type] || !my_listeners[type].length)
        return;

    for (let i = 0; i < my_listeners[type].length; i++)
    {
        var handler = my_listeners[type][i];
        handler.obj_id.removeEventListener(type, handler.fn, handler.options);
    }

    evtLogMessage("(JS) RmByType removed "+ my_listeners[type].length +" [" + type + "] listeners");

    my_listeners[type] = [];
}
function MyRemoveEventListenerByIdAndType(obj_id, type)
{
    var attr_id = obj_id.getAttribute("id");
    var letter = "";
    if (attr_id.startsWith("Tile"))
        letter = getTileLetter(obj_id);

    if (!my_listeners[type] || !my_listeners[type].length)
        return;

    for (let i = 0; i < my_listeners[type].length; i++)
    {
        if (my_listeners[type][i].attr_id == attr_id)
        {
            var handler = my_listeners[type][i];
            handler.obj_id.removeEventListener(type, handler.fn, handler.options);
            // remove the item from the list
            my_listeners[type].splice(i, 1);
            evtLogMessage("RmByIdAndType for '" + attr_id + letter + "' [" + type + "/" + my_listeners[type].length +"]");
            break;
        }
    }
}

var removeAllEventListener = function (type) {
    if (!listeners[type] || !listeners[type].length)
        return;

    for (let i = 0; i < listeners[type].length; i++)
        window.removeEventListener(type, listeners[type][i]);

    listeners[type] = [];
}


// Remove event listeners to prevent an unlimited number of
// event listeners when switching between multiple games
export async function CleanupDragAndDrop() {
    evtLogMessage("(JS) CleanupDragAndDrop");

    /* not sure this stuff was actually working...
    removeAllEventListener('dragstart');
    removeAllEventListener('dragover');
    removeAllEventListener('dragend');
    removeAllEventListener('drop');
    removeAllEventListener('contextmenu');
    */

    MyRemoveAllEventListeners();
}



// Track used event listeners
var listeners = {};

var originalEventListener = window.addEventListener;
window.addEventListener = function (type, fn, options) {
    if (!listeners[type])
        listeners[type] = [];

    listeners[type].push(fn);
    return originalEventListener(type, fn, options);
}
