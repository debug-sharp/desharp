var Desharp = (function () {
    var Desharp = function (unicodeIndexes) {
        if (!unicodeIndexes.length || Desharp['Instance']) return;
        var scope = this;
        Desharp['Instance'] = scope;
        scope._mouseInside = false;
        scope._oldIe = /MSIE [5-8]/gi.test(navigator.userAgent);
        scope._cookieName = Desharp['COOKIE_NAME'];
        scope._moveAndResizeSizes = [0, 0, 0, 0];
        scope._headMouseDown = false;
        scope._cornerResizerMouseDown = false;
        scope._leftResizerMouseDown = false;
        scope._bottomResizerMouseDown = false;
        scope._sizes = [0, 0, 100, 100];
        scope._clickSpans = {};
        scope._dumpDivs = {};
        scope._cont = scope._elm('div', { id: scope._cookieName + '-cont' });
        scope._inner = scope._elm('div', { id: scope._cookieName + '-inner' });
        scope._head = scope._elm('div', { id: scope._cookieName + '-head' });
        scope._head.innerHTML = 'Dumps';
        scope._body = scope._elm('div', { id: scope._cookieName + '-body' });
        scope._cornerResizer = scope._elm('div', { id: scope._cookieName + '-corner' });
        scope._leftResizer = scope._elm('div', { id: scope._cookieName + '-left' });
        scope._bottomResizer = scope._elm('div', { id: scope._cookieName + '-bottom' });
        scope._body.innerHTML = String.fromCharCode.apply(window, unicodeIndexes);
        scope._setUpElmsTogether();
        scope._setUpSizes();
        scope._setUpContent();
        if (document.body == null) {
            window.onload = function () {
                scope._start();
            }
        } else {
            scope._start();
        }
    };
    Desharp['Instance'] = null;
    Desharp['COOKIE_NAME'] = 'desharp-dumps';
    Desharp.OPENED_CSS_CLASS = ' opened';
    Desharp.CLICK_CSS_CLASS_BEGIN = 'click click-';
    Desharp.DUMP_CSS_CLASS_BEGIN = 'dump dump-';
    Desharp.HTML_CODE_CSS_CLASS = 'html-code';
    Desharp.INNER_PADDING = 5;
    Desharp.prototype = {
        _setUpElmsTogether: function () {
            var scope = this;
            scope._head = scope._append(scope._inner, scope._head);
            scope._body = scope._append(scope._inner, scope._body);
            scope._inner = scope._append(scope._cont, scope._inner);
            scope._cornerResizer = scope._append(scope._cont, scope._cornerResizer);
            scope._leftResizer = scope._append(scope._cont, scope._leftResizer);
            scope._bottomResizer = scope._append(scope._cont, scope._bottomResizer);
        },
        _setUpSizes: function () {
            var scope = this,
				cookieSizes = scope._trim(scope._getCookie(scope._cookieName)),
                sizes = [];
            if (cookieSizes) {
                sizes = cookieSizes.split('_');
                for (var i = 0, l = sizes.length; i < l; i++) scope._sizes[i] = parseInt(sizes[i], 10);
            }
            scope._setCookie(scope._cookieName, scope._sizes.join('_'));
        },
        _setUpContent: function () {
            var scope = this,
				elms = scope._body.getElementsByTagName("span"),
				elm = {},
				cls = '',
				clsPos = 0,
                divKey = '',
				clsBegin = Desharp.CLICK_CSS_CLASS_BEGIN,
				htmlCodeCls = Desharp.HTML_CODE_CSS_CLASS;
            for (var i = 0, l = elms.length; i < l; i += 1) {
                elm = elms[i];
                cls = elm.className;
                clsPos = cls.indexOf(clsBegin);
                if (clsPos > -1) {
                    scope._clickSpans[scope._getClickOrDumpEmlClassId(elm, cls, clsPos, clsBegin)] = elm;
                } else if (cls.indexOf(htmlCodeCls) > -1) {
                    elm.innerHTML = elm.innerHTML
						.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                }
            };
            elms = scope._body.getElementsByTagName("div");
            clsBegin = Desharp.DUMP_CSS_CLASS_BEGIN;
            for (var i = 0, l = elms.length; i < l; i += 1) {
                elm = elms[i];
                cls = elm.className;
                clsPos = cls.indexOf(clsBegin);
                if (clsPos > -1) {
                    divKey = scope._getClickOrDumpEmlClassId(elm, cls, clsPos, clsBegin);
                    if (typeof (scope._dumpDivs[divKey]) == 'undefined') {
                        scope._dumpDivs[divKey] = [elm];
                    } else {
                        scope._dumpDivs[divKey].push(elm);
                    }
                }
            };
        },
        _start: function () {
            var scope = this;
            scope._cont = scope._append(document.body, scope._cont);
            scope._setUpEvents();
            scope._styleElms();
            scope._openFirstLevelItems();
        },
        _openFirstLevelItems: function () {
            var scope = this,
				firstDivs = scope._body.getElementsByTagName("div"),
				firstSpans = [],
				firstSpan = {},
				clickClsBegin = Desharp.CLICK_CSS_CLASS_BEGIN;
            if (firstDivs.length > 8) return;
            for (var i = 0, l = firstDivs.length; i < l; i += 1) {
                firstSpans = firstDivs[i].getElementsByTagName("span");
                for (var j = 0, k = firstSpans.length; j < k; j += 1) {
                    firstSpan = firstSpans[j];
                    if (firstSpan.className.indexOf(clickClsBegin) > 0) {
                        firstSpan.click();
                    }
                }
            }
        },
        _getClickOrDumpEmlClassId: function (elm, cls, clsPos, clsBegin) {
            cls = cls.substr(clsPos);
            clsPos = cls.indexOf(" ", clsBegin.length);
            return cls.substr(clsBegin.length, clsPos > -1 ? clsPos : cls.length);
        },
        _setUpEvents: function () {
            var scope = this,
				onmousedown = 'onmousedown',
				onmousemove = 'onmousemove',
				onmouseup = 'onmouseup';
            scope._head[onmousedown] = function (e) {
                var xy = scope._getCoords(e);
                scope._headMouseDown = true;
                scope._moveAndResizeSizes[0] = xy.pageX - scope._sizes[0];
                scope._moveAndResizeSizes[1] = xy.pageY - scope._sizes[1];
            };
            scope._head[onmousemove] = function () {
                scope._styleElms();
            };
            scope._head[onmouseup] = function () {
                scope._headMouseDown = false;
                scope._setCookie(scope._cookieName, scope._sizes.join('_'));
            };
            scope._cornerResizer[onmousedown] = function (e) {
                var xy = scope._getCoords(e);
                scope._cornerResizerMouseDown = true;
                scope._moveAndResizeSizes[2] = xy.pageX - scope._sizes[2];
                scope._moveAndResizeSizes[3] = xy.pageY - scope._sizes[3];
            };
            scope._cornerResizer[onmouseup] = function () {
                scope._cornerResizerMouseDown = false;
                scope._setCookie(scope._cookieName, scope._sizes.join('_'));
            };
            scope._leftResizer[onmousedown] = function (e) {
                var xy = scope._getCoords(e);
                scope._leftResizerMouseDown = true;
                scope._moveAndResizeSizes[2] = xy.pageX - scope._sizes[2];
            };
            scope._leftResizer[onmouseup] = function () {
                scope._leftResizerMouseDown = false;
                scope._setCookie(scope._cookieName, scope._sizes.join('_'));
            };
            scope._bottomResizer[onmousedown] = function (e) {
                var xy = scope._getCoords(e);
                scope._bottomResizerMouseDown = true;
                scope._moveAndResizeSizes[3] = xy.pageY - scope._sizes[3];
            };
            scope._bottomResizer[onmouseup] = function () {
                scope._bottomResizerMouseDown = false;
                scope._setCookie(scope._cookieName, scope._sizes.join('_'));
            };
            document[onmousemove] = function (e) {
                scope._documentOnMouseMoveHandler(e);
            };
            scope._body.onclick = function (e) {
                e = e || window.event;
                scope._bodyClickHandler(e);
            };
            scope._headSize = scope._head.offsetHeight;
        },
        _getCoords: function (e) {
            e = e || window.event;
            if (this._oldIe) {
                var doc = document,
					docElm = doc['documentElement'],
					docBody = doc['body'],
					docScrolls = [],
					scrollTop = 'scrollTop',
					scrollLeft = 'scrollLeft';
                if (docElm && docElm[scrollTop]) {
                    docScrolls = [
						docElm[scrollLeft],
						docElm[scrollTop]
                    ];
                } else {
                    docScrolls = [
						docBody[scrollLeft],
						docBody[scrollTop]
                    ];
                }
                return {
                    pageX: e['clientX'] + docScrolls[0],
                    pageY: e['clientY'] + docScrolls[1]
                };
            } else {
                return {
                    pageX: e.pageX,
                    pageY: e.pageY
                };
            }
        },
        _documentOnMouseMoveHandler: function (e) {
            var scope = this, xy = {};
            if (
				scope._headMouseDown ||
				scope._cornerResizerMouseDown ||
				scope._leftResizerMouseDown ||
				scope._bottomResizerMouseDown
			) {
                xy = scope._getCoords(e);
                if (scope._headMouseDown) {
                    scope._sizes[0] = xy.pageX - scope._moveAndResizeSizes[0];
                    scope._sizes[1] = xy.pageY - scope._moveAndResizeSizes[1];
                }
                if (scope._cornerResizerMouseDown) {
                    scope._sizes[2] = xy.pageX - scope._moveAndResizeSizes[2];
                    scope._sizes[3] = xy.pageY - scope._moveAndResizeSizes[3];
                }
                if (scope._leftResizerMouseDown) {
                    scope._sizes[2] = xy.pageX - scope._moveAndResizeSizes[2];
                }
                if (scope._bottomResizerMouseDown) {
                    scope._sizes[3] = xy.pageY - scope._moveAndResizeSizes[3];
                }
                scope._styleElms();
            }
        },
        _bodyClickHandler: function (e) {
            var scope = this,
				srcElm = scope._getEventSourceElm(e),
				cls = srcElm.className,
				clsBegin = Desharp.CLICK_CSS_CLASS_BEGIN,
				clsPos = cls.indexOf(clsBegin),
				dumpElms = [],
				dumpElm = {};
            if (clsPos > -1) {
                clsBegin = Desharp.OPENED_CSS_CLASS;
                if (cls.indexOf(clsBegin) > -1) {
                    srcElm.className = srcElm.className.replace(clsBegin, '');
                } else {
                    srcElm.className = srcElm.className + clsBegin;
                }
                dumpElms = scope._dumpDivs[scope._getClickOrDumpEmlClassId(srcElm, cls, clsPos, clsBegin)];
                for (var i = 0, l = dumpElms.length; i < l; i += 1) {
                    dumpElm = dumpElms[i];
                    if (dumpElm) {
                        cls = dumpElm.className;
                        if (cls.indexOf(clsBegin) > -1) {
                            dumpElm.className = dumpElm.className.replace(clsBegin, '');
                        } else {
                            dumpElm.className = dumpElm.className + clsBegin;
                        }
                    }
                }
            }
        },
        _getEventSourceElm: function (e) {
            var result = e.target ? e.target : (e.srcElement ? e.srcElement : null);
            if (result.nodeType == 3) result = result.parentNode; // Safari bug by clicking on text node
            return result;
        },
        _styleElms: function () {
            var scope = this,
                x = scope._sizes[0],
                y = scope._sizes[1],
                w = scope._sizes[2],
                h = scope._sizes[3],
				p = Desharp.INNER_PADDING,
				p2 = p * 2,

				width = 'width',
				height = 'height',

                constStyle = scope._cont.style,
                bodyStyle = scope._body.style,
                cornerStyle = scope._cornerResizer.style;

            scope._styleElmProp(constStyle, 'top', y);
            scope._styleElmProp(constStyle, 'left', x);
            scope._styleElmProp(constStyle, width, w);
            scope._styleElmProp(constStyle, height, h + 5);
            scope._styleElmProp(scope._head.style, width, w - p2 + 2);
            scope._styleElmProp(bodyStyle, width, w - p2 - 10/* 5 - body padding left and right*/);
            scope._styleElmProp(bodyStyle, height, h - scope._headSize - p - 5/* 5 - body padding bottom*/);
        },
        _styleElmProp: function (styleObj, propName, propValue) {
            if (this._oldIe) {
                styleObj[propName] = propValue;
            } else {
                styleObj[propName] = propValue + 'px';
            }
        },
        _trim: function (a, b) {
            var c, d = 0, e = 0;
            a += "";
            if (b) {
                b += "";
                c = b.replace(/([\[\]\(\)\.\?\/\*\{\}\+\$\^\:])/g, "$1");
            } else {
                c = " \n\r\t\u000c\u000b\u00a0\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u200b\u2028\u2029\u3000";
            }
            d = a.length;
            for (e = 0; e < d; e++) {
                if (c.indexOf(a.charAt(e)) === -1) {
                    a = a.substring(e);
                    break;
                }
            }
            d = a.length;
            for (e = d - 1; e >= 0; e--) {
                if (c.indexOf(a.charAt(e)) === -1) {
                    a = a.substring(0, e + 1);
                    break;
                }
            }
            return c.indexOf(a.charAt(0)) === -1 ? a : '';
        },
        _elm: function (name, attributes) {
            var elm = document.createElement(name);
            attributes = attributes || {};
            for (var name in attributes) {
                elm.setAttribute(name, attributes[name]);
            }
            return elm;
        },
        _append: function (parent, child) {
            if (this._oldIe) {
                return parent.insertAdjacentElement('beforeEnd', child);
            } else {
                return parent.appendChild(child);
            }
        },
        _getCookie: function (name) {
            var resultCookie = '',
                docCookieStr = document.cookie,
                docCookieStrArr = docCookieStr.split(';'),
                docCookieStrItem = '',
                cookieDelimiterStr = '__JS_COOKIE_DELIMITER__';
            for (var i = 0, l = docCookieStrArr.length; i < l; i++) {
                docCookieStrItem = this._trim(docCookieStrArr[i]);
                if (docCookieStrItem.indexOf('expires') === 0 || docCookieStrItem.indexOf('path') === 0 || docCookieStrItem.indexOf('domain') === 0 || docCookieStrItem.indexOf('max-age') === 0 || docCookieStrItem.indexOf('secure') === 0) {
                    docCookieStrArr[i] = docCookieStrItem;
                } else if (i > 0) {
                    docCookieStrArr[i] = cookieDelimiterStr + docCookieStrItem;
                }
            }
            docCookieStr = docCookieStrArr.join('; ');
            docCookieStrArr = docCookieStr.split(cookieDelimiterStr);
            for (var i = 0, l = docCookieStrArr.length; i < l; i++) {
                docCookieStrItem = this._trim(docCookieStrArr[i]);
                if (docCookieStrItem.indexOf(name) === 0) {
                    resultCookie = docCookieStrArr[i];
                    break;
                }
            }
            if (resultCookie.indexOf(';') > -1) {
                resultCookie = resultCookie.substr(0, resultCookie.indexOf(';'));
            }
            resultCookie = resultCookie.substr(resultCookie.indexOf('=') + 1);
            resultCookie = this._trim(decodeURIComponent(resultCookie));
            return resultCookie;
        },
        _setCookie: function (name, value, exdays) {
            var exdate = new Date(),
                newCookieRawValue = '',
                explDomain = [],
                domain = '';
            exdays = exdays || 365;
            exdate.setDate(exdate.getDate() + exdays);
            newCookieRawValue = name + "=" + encodeURIComponent(value);
            newCookieRawValue += '; path=/';
            /* use only for multiple domains in third level */
            explDomain = location.hostname.split('.');
            if (explDomain.length == 3) {
                explDomain[0] = '';
                domain = explDomain.join('.');
                newCookieRawValue += '; domain=' + domain;
            } else {
                newCookieRawValue += '; domain=' + location.hostname;
            }
            if (exdays) {
                newCookieRawValue += '; expires=' + exdate.toUTCString();
            }
            document.cookie = newCookieRawValue;
        }
    };
    return Desharp;
})();