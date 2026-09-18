theme = hicolor
themedir = $(datadir)/icons/$(theme)
theme_icons = \
	apps,Linux/monodevelop.svg \
	apps,Linux/monodevelop-16.png \
	apps,Linux/monodevelop-22.png \
	apps,Linux/monodevelop-24.png \
	apps,Linux/monodevelop-32.png \
	apps,Linux/monodevelop-48.png \
	apps,Linux/monodevelop-64.png \
	apps,Linux/monodevelop-128.png \
	apps,Linux/monodevelop-256.png \
	apps,Linux/monodevelop-512.png

install_icon_exec = $(top_srcdir)/theme-icons/icon-theme-installer \
	-t "$(theme)" \
	-s "$(srcdir)" \
	-d "x$(DESTDIR)" \
	-b "$(themedir)" \
	-m "$(mkinstalldirs)" \
	-x "$(INSTALL_DATA)"

install-data-local:
	@-$(install_icon_exec) -i $(theme_icons)

uninstall-hook:
	@-$(install_icon_exec) -u $(theme_icons)

EXTRA_DIST = \
	$(wildcard Linux/*.png) \
	Linux/monodevelop.svg \
	$(wildcard Mac/png/*.png) \
	$(wildcard Mac/*.icns) \
	$(wildcard Windows/*.ico) \
	monodevelop-source.svg \
	icon-theme-installer

