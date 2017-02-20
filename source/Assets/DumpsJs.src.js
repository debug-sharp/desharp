var Desharp = (function () {
    var Desharp = function (unicodeIndexes) {
        if (!unicodeIndexes.length) return;
        var content = String.fromCharCode.apply(window, unicodeIndexes);
        this.headMouseDown = [false, 0, 0];
        this.resizerMouseDown = [false, 0, 0];
        this._sizes = [0, 0, 100, 100];
        this.cont = this.elm('div', Desharp.contStyles, { id: 'desharp-dumps-cont' });
        this._head = this.elm('div', Desharp.headStyles, { id: 'desharp-dumps-head' });
        this._body = this.elm('div', Desharp.bodyStyles, { id: 'desharp-dumps-body' }, content);
        this.resizer = this.elm('div', Desharp.resizerStyles, { id: 'desharp-dumps-resizer' });
        this.styles = this.elm('style', {}, { type: 'text/css' }, Desharp.STYLES);
        this.setUpElmsTogether();
        this.setUpSizes();
        this.setUpEvents();
        var htmlCodeSpans = this._body.getElementsByTagName("span"),
            codeSpan;
        for (var i = 0, l = htmlCodeSpans.length; i < l; i += 1) {
            codeSpan = htmlCodeSpans[i];
            if (codeSpan.className.indexOf('html-code') > -1) {
                codeSpan.innerHTML = codeSpan.innerHTML
                    .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
            }
        };
        if (document.body == null) {
            var scope = this;
            window.onload = function () {
                scope.cont = scope._append(document.body, scope.cont);
            }
        } else {
            this.cont = this._append(document.body, this.cont);
        }
    };
    Desharp.COOKIE_NAME = 'desharp-dumps-window';
    Desharp.STYLES = '#desharp-dumps-body{font-family:\'Consolas\',courier new;font-weight:bold;}'
	+ ' #desharp-dumps-body span.table{color:rgb(100,255,255);}'
	+ ' #desharp-dumps-body span.column{color:rgb(0,200,255);}'
	+ ' #desharp-dumps-body span.type{color:#999;}'
	+ ' #desharp-dumps-body span.document{padding:0;margin:0;}'
	+ ' #desharp-dumps-body span.string{color:rgb(0,255,139);}'
	+ ' #desharp-dumps-body span.html-code{color:orange;}'
	+ ' #desharp-dumps-body span.int{color:rgb(255,0,48);}'
	+ ' #desharp-dumps-body span.boolean{color:rgb(255,48,48);}'
	+ ' #desharp-dumps-body span.int32{color:rgb(255,0,80);}'
	+ ' #desharp-dumps-body span.int64{color:rgb(255,48,96);}'
	+ ' #desharp-dumps-body span.double{color:rgb(255,0,213);}'
	+ ' #desharp-dumps-body span.long{color:rgb(142,0,255);}'
	+ ' #desharp-dumps-body span.dbnull{color:rgb(255,0,255);}';
    Desharp.contStyles = {
        position: 'absolute',
        'z-index': 9999990,
        background: '#000',
        color: '#fff',
        overflow: 'hidden'
    };
    Desharp.headStyles = {
        position: 'absolute',
        'z-index': 9999991,
        width: '100%',
        height: '30px',
        background: '#888',
        cursor: 'move',
        overflow: 'hidden'
    };
    Desharp.bodyStyles = {
        position: 'absolute',
        'z-index': 9999992,
        top: '30px',
        width: '100%',
        overflow: 'auto'
    };
    Desharp.resizerStyles = {
        position: 'absolute',
        'z-index': 9999993,
        width: '10px',
        height: '10px',
        background: '#fff',
        cursor: 'se-resize',
        overflow: 'hidden'
    };
    Desharp.prototype = {
        setUpElmsTogether: function () {
            this._head = this._append(this.cont, this._head);
            this._body = this._append(this.cont, this._body);
            this.resizer = this._append(this.cont, this.resizer);
            this.styles = this._append(this.cont, this.styles);
        },
        setUpSizes: function () {
            var cookieSizes = this.trim(this.getCookie(Desharp.COOKIE_NAME)),
                sizes = [];
            if (cookieSizes) {
                sizes = cookieSizes.split('_');
                for (var i = 0, l = sizes.length; i < l; i++) this._sizes[i] = parseInt(sizes[i], 10);
            }
            this.styleElms();
            this.setCookie(Desharp.COOKIE_NAME, this._sizes.join('_'));
        },
        setUpEvents: function () {
            var scope = this;
            this._head.onmousedown = function (e) {
                scope.headMouseDown = [true, e.pageX - scope._sizes[0], e.pageY - scope._sizes[1]];
            };
            this._head.onmousemove = function (e) {
                scope.styleElms();
            };
            this._head.onmouseup = function (e) {
                scope.headMouseDown[0] = false;
                scope.setCookie(Desharp.COOKIE_NAME, scope._sizes.join('_'));
            };

            this.resizer.onmousedown = function (e) {
                scope.resizerMouseDown = [true, e.pageX - scope._sizes[2], e.pageY - scope._sizes[3]];
            };
            this.resizer.onmouseup = function (e) {
                scope.resizerMouseDown[0] = false;
                scope.setCookie(Desharp.COOKIE_NAME, scope._sizes.join('_'));
            };
            document.onmousemove = function (e) {
                if (scope.headMouseDown[0]) {
                    scope._sizes[0] = e.pageX - scope.headMouseDown[1];
                    scope._sizes[1] = e.pageY - scope.headMouseDown[2];
                }
                if (scope.resizerMouseDown[0]) {
                    scope._sizes[2] = e.pageX - scope.resizerMouseDown[1];
                    scope._sizes[3] = e.pageY - scope.resizerMouseDown[2];
                }
                if (scope.headMouseDown[0] || scope.resizerMouseDown[0]) scope.styleElms();
            };
        },
        styleElms: function () {
            var px = 'px',
                x = this._sizes[0],
                y = this._sizes[1],
                w = this._sizes[2],
                h = this._sizes[3],

                constStyle = this.cont.style,
                bodyStyle = this._body.style,
                resizerStyle = this.resizer.style;

            constStyle.top = y + px;
            constStyle.left = x + px;
            constStyle.width = w + px;
            constStyle.height = h + px;

            this._head.style.width = w + px;

            bodyStyle.width = w + px;
            bodyStyle.height = (h - 30) + px;

            resizerStyle.top = (h - 11) + px;
            resizerStyle.left = (w - 11) + px;
        },
        trim: function (a, b) {
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
        elm: function (name, styles, attributes, content) {
            var elm = document.createElement(name);
            styles = styles || {};
            attributes = attributes || {};
            for (var name in attributes) {
                elm.setAttribute(name, attributes[name]);
            }
            for (var name in styles) {
                elm.style[name] = styles[name];
            }
            if (content) elm.innerHTML = content;
            return elm;
        },
        _append: function (parent, child) {
            if (parent.appendChild) {
                return parent.appendChild(child);
            } else {
                return parent.insertAdjacentElement('beforeEnd', child);
            }
        },
        getCookie: function (name) {
            var resultCookie = '',
                docCookieStr = document.cookie,
                docCookieStrArr = docCookieStr.split(';'),
                docCookieStrItem = '',
                cookieDelimiterStr = '__JS_COOKIE_DELIMITER__';
            for (var i = 0, l = docCookieStrArr.length; i < l; i++) {
                docCookieStrItem = this.trim(docCookieStrArr[i]);
                if (docCookieStrItem.indexOf('expires') === 0 || docCookieStrItem.indexOf('path') === 0 || docCookieStrItem.indexOf('domain') === 0 || docCookieStrItem.indexOf('max-age') === 0 || docCookieStrItem.indexOf('secure') === 0) {
                    docCookieStrArr[i] = docCookieStrItem;
                } else if (i > 0) {
                    docCookieStrArr[i] = cookieDelimiterStr + docCookieStrItem;
                }
            }
            docCookieStr = docCookieStrArr.join('; ');
            docCookieStrArr = docCookieStr.split(cookieDelimiterStr);
            for (var i = 0, l = docCookieStrArr.length; i < l; i++) {
                docCookieStrItem = this.trim(docCookieStrArr[i]);
                if (docCookieStrItem.indexOf(name) === 0) {
                    resultCookie = docCookieStrArr[i];
                    break;
                }
            }
            if (resultCookie.indexOf(';') > -1) {
                resultCookie = resultCookie.substr(0, resultCookie.indexOf(';'));
            }
            resultCookie = resultCookie.substr(resultCookie.indexOf('=') + 1);
            resultCookie = this.trim(decodeURIComponent(resultCookie));
            return resultCookie;
        },
        setCookie: function (name, value, exdays) {
            var exdate = new Date(),
                newCookieRawValue = '',
                explDomain = [],
                domain = '';
            exdays = exdays || 365;
            exdate.setDate(exdate.getDate() + exdays);
            newCookieRawValue = name + "=" + encodeURIComponent(value) + '; ';
            newCookieRawValue += 'path=/; ';
            /* use only for multiple domains in third level */
            explDomain = location.host.split('.');
            explDomain[0] = '';
            domain = explDomain.join('.');
            newCookieRawValue += 'domain=' + domain + '; ';
            if (exdays) {
                newCookieRawValue += 'expires=' + exdate.toUTCString() + '; ';
            }
            return document.cookie = newCookieRawValue;
        }
    };
    return Desharp;
})();