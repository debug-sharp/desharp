var init = function () {
	var a = document.querySelectorAll('a.desharp-dump-control'),
		d = 'desharp-dump-dtls',
		e = function (c) {
			var o = c.querySelectorAll('span.desharp-dump-msg b');
			c.setAttribute("href", "https://www.google.com/webhp?hl=en&sourceid=c-sharp-debug&q=" + String(o[0].innerHTML).replace(/\t\r\n/g, " ").replace(/ /g, "+"));
			c.onclick = function (e) {
				e = e || window.event;
				if (c.parentNode.className.indexOf(d) > -1) {
					c.parentNode.className = c.parentNode.className.replace(d, '');
				} else {
					c.parentNode.className += ' ' + d;
				}
				e.preventDefault();
				return false;
			};
			return true;
		};
	for (var b = 0; b < a.length; b++) {
		e(a[b]);
	};
	var f = document.querySelectorAll('body.debug-exception table.desharp-dump-dtls');
	if (f.length) {
		var g = f[0].querySelectorAll('tbody tr'),
			h = g[0].querySelectorAll('td.flnm'),
			i = g[0].querySelectorAll('td.mthd'),
			j = (f[0].offsetWidth - h[0].offsetWidth - 60 - 50) + 'px';
		for (var k = 0, l = g.length; k < l; k++) {
			var m = g[k].querySelectorAll('td.mthd'),
				n = m[0].querySelectorAll('i');
			m[0].setAttribute('title', m[0].innerHTML);
			n[0].style.width = j;
			n[0].style['width'] = j;
		}
		i[0].className = "mthd";
	}
}