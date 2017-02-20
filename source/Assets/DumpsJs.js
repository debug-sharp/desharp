var Debug = (function () {
	var Debug = function (unicodeIndexes) {
		if (!unicodeIndexes.length) return;
		var content = String.fromCharCode.apply(window, unicodeIndexes);
		this.headMouseDown = [false, 0, 0];
		this.resizerMouseDown = [false, 0, 0];
		this.sizes = [0, 0, 100, 100];
		this.cont = this.elm('div', Debug.contStyles, { id: 'dot-net-debug-cont' });
		this.head = this.elm('div', Debug.headStyles, { id: 'dot-net-debug-head' });
		this.body = this.elm('div', Debug.bodyStyles, { id: 'dot-net-debug-body' }, content);
		this.resizer = this.elm('div', Debug.resizerStyles, { id: 'dot-net-debug-resizer' });
		this.styles = this.elm('style', {}, { type: 'text/css' }, Debug.STYLES);
		this.setUpElmsTogether();
		this.setUpSizes();
		this.setUpEvents();
		var htmlCodeSpans = this.body.querySelectorAll("span.html-code"), codeSpan;
		for (var i = 0, l = htmlCodeSpans.length; i < l; i += 1) {
			codeSpan = htmlCodeSpans[i];
			codeSpan.innerHTML = codeSpan.innerHTML.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
		}
		document.body.appendChild(this.cont);
	}
	Debug.COOKIE_NAME = 'dot-net-debug-window';
	Debug.STYLES = '#dot-net-debug-body{font-family:\'Consolas\',courier new;font-weight:bold;}'
	+ ' #dot-net-debug-body span.table{color:rgb(100,255,255);}'
	+ ' #dot-net-debug-body span.column{color:rgb(0,200,255);}'
	+ ' #dot-net-debug-body span.type{color:#999;}'
	+ ' #dot-net-debug-body span.document{padding:0;margin:0;}'
	+ ' #dot-net-debug-body span.string{color:rgb(0,255,139);}'
	+ ' #dot-net-debug-body span.html-code{color:orange;}'
	+ ' #dot-net-debug-body span.int{color:rgb(255,0,48);}'
	+ ' #dot-net-debug-body span.boolean{color:rgb(255,48,48);}'
	+ ' #dot-net-debug-body span.int32{color:rgb(255,0,80);}'
	+ ' #dot-net-debug-body span.int64{color:rgb(255,48,96);}'
	+ ' #dot-net-debug-body span.double{color:rgb(255,0,213);}'
	+ ' #dot-net-debug-body span.long{color:rgb(142,0,255);}'
	+ ' #dot-net-debug-body span.dbnull{color:rgb(255,0,255);}';
	Debug.contStyles = {
		position: 'absolute',
		'z-index': 9999990,
		background: '#000',
		color: '#fff',
		overflow: 'hidden'
	};
	Debug.headStyles = {
		position: 'absolute',
		'z-index': 9999991,
		width: '100%',
		height: '30px',
		background: '#888',
		cursor: 'move',
		overflow: 'hidden'
	};
	Debug.bodyStyles = {
		position: 'absolute',
		'z-index': 9999992,
		top: '30px',
		width: '100%',
		overflow: 'auto'
	};
	Debug.resizerStyles = {
		position: 'absolute',
		'z-index': 9999993,
		width: '10px',
		height: '10px',
		background: '#fff',
		cursor: 'se-resize',
		overflow: 'hidden'
	};
	Debug.prototype = {
		setUpElmsTogether: function () {
			this.cont.appendChild(this.head);
			this.cont.appendChild(this.body);
			this.cont.appendChild(this.resizer);
			this.cont.appendChild(this.styles);
		},
		setUpSizes: function () {
			var cookieSizes = this.trim(this.getCookie(Debug.COOKIE_NAME)),
				sizes = [];
			if (cookieSizes) {
				sizes = cookieSizes.split('_');
				for (var i = 0, l = sizes.length; i < l; i++) this.sizes[i] = parseInt(sizes[i], 10);
			}
			this.styleElms();
			this.setCookie(Debug.COOKIE_NAME, this.sizes.join('_'));
		},
		setUpEvents: function () {
			this.head.addEventListener('mousedown', function (e) {
				this.headMouseDown = [true, e.pageX - this.sizes[0], e.pageY - this.sizes[1]];
			}.bind(this));
			this.head.addEventListener('mousemove', function (e) {

				this.styleElms();
			}.bind(this));
			this.head.addEventListener('mouseup', function (e) {
				this.headMouseDown[0] = false;
				this.setCookie(Debug.COOKIE_NAME, this.sizes.join('_'));
			}.bind(this));

			this.resizer.addEventListener('mousedown', function (e) {
				this.resizerMouseDown = [true, e.pageX - this.sizes[2], e.pageY - this.sizes[3]];
			}.bind(this));
			this.resizer.addEventListener('mouseup', function (e) {
				this.resizerMouseDown[0] = false;
				this.setCookie(Debug.COOKIE_NAME, this.sizes.join('_'));
			}.bind(this));
			document.addEventListener('mousemove', function (e) {
				if (this.headMouseDown[0]) {
					this.sizes[0] = e.pageX - this.headMouseDown[1];
					this.sizes[1] = e.pageY - this.headMouseDown[2];
				}
				if (this.resizerMouseDown[0]) {
					this.sizes[2] = e.pageX - this.resizerMouseDown[1];
					this.sizes[3] = e.pageY - this.resizerMouseDown[2];
				}
				if (this.headMouseDown[0] || this.resizerMouseDown[0]) this.styleElms();
			}.bind(this));
		},
		styleElms: function () {
			var px = 'px',
				x = this.sizes[0],
				y = this.sizes[1],
				w = this.sizes[2],
				h = this.sizes[3];

			this.cont.style.top = y + px;
			this.cont.style.left = x + px;
			this.cont.style.width = w + px;
			this.cont.style.height = h + px;

			this.head.style.width = w + px;

			this.body.style.width = w + px;
			this.body.style.height = (h - 30) + px;

			this.resizer.style.top = (h - 11) + px;
			this.resizer.style.left = (w - 11) + px;
		},
		getEvaluated: function (val) {
			var result;
			var failResult = {
				success: false,
				data: val
			};
			if (String(val).length > 0) {
				try {
					result = {
						success: true,
						data: eval('(function(){return (' + val + ');})();')
					}
				} catch (e) {
					result = failResult;
					result.message = '[Evaluation error] ' + e.message;
				}
			} else {
				result = failResult;
				result.message = '[Evaluation error] No data from: ' + arguments.callee.caller + '  from: ' + arguments.callee.caller.caller;
			}
			if (!result.success) {
				console.log(result);
			}
			return result;
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
			// use only for multiple domains in third level
			explDomain = location.host.split('.');
			explDomain[0] = '';
			domain = explDomain.join('.');
			newCookieRawValue += 'domain=' + domain + '; ';
			if (exdays) {
				newCookieRawValue += 'expires=' + exdate.toUTCString() + '; ';
			}
			return document.cookie = newCookieRawValue;
		}
	}
	return Debug;
})();