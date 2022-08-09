## Config Files

This folder contains examples of `.conf` files, which the application uses to set custom keyboard layouts.
Each config file contains the layout the application will set the keyboard to when a particular window is in focus.
### Naming
 The name of the config determines which window it will be used for. For example, `Google Chrome.conf` will be used when (you guessed it) Google Chrome is focused.

`default.conf` is special, in that it will be used for any applications that don't have their own configs.

### Formatting

Configs **must** be formatted in a specific way to function properly.
```
#hashes can be used to comment
KEYCODE\tHEXVALUES\n
```
(I'd recommend using a tool such as [this](https://onlinestringtools.com/escape-string) to check `\t` and `\n` have been used correctly).
For example:
```
#red
Esc ff0000
#green
F1	00ff00
#blue
F2	0000ff
```
- The keycodes are case sensitive and must be exactly the same as in `kym-Library\data.cpp`. Invalid keycodes will be ignored.
- The hex values must be a single string of 6 characters (each character 0-9 or a-f), in the pattern RRGGBB.
	- If the hex codes are invalid, the key will be set to red by default.

### Note
If the application is built using Visual Studio, these example files should automatically be moved into the build location automatically, allowing the application to use them.