## Config Files

This folder contains examples of `.base` and `.conf` files, which the application uses to set custom keyboard layouts.
Each base file contains a full keyboard layout to be used as a background, whereas the config files contain a layer to overule the base. The config files do not have to reference every key, and so will only overide the specified keys, leaving the base's layout visible for said keys.
Each config file corresponds to an application, and the layer is applied when said application is in focus.
### Naming
 The name of the config determines which window it will be used for. For example, `Google Chrome.conf` will be used when (you guessed it) Google Chrome is focused.

 `Google Chrome_LCtrl.conf` would be used when the Left Control key is pressed with Chrome open.

 Files starting with `_`, such as `_1.conf` are temporary files which hold the information required to easily 'remove' the current layer. Temporary files follow the naming pattern of `_<profile><x>.conf` where `x` is `a` if the file is for a hotkey, or otherwise blank.

`Default.base` is special, in that it will be used for any applications that don't have their own configs.

### Formatting

The application handles all file creation/editing, and so no manual interaction with the files should be required. However, below is an overview of the files which may be useful should you want to create them manually, or use/edit either the application or library's source code.

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

Bases contain a value for every key, and so no identifiers are used. The file **must** be formatted in a specific way to function properly. The order of the values must be in a specififc way to function (this order is due to the hardware itself, and so cannot be changed).

More examples of these files can be found [here](https://github.com/Razzula/keymeleon/tree/b33e9231d6031c331e52dc89960bd35050ec1721), such as [test.base](https://github.com/Razzula/keymeleon/blob/b33e9231d6031c331e52dc89960bd35050ec1721/keymeleon-console/test.base) which shows the row configurations.

### Note
If the application is built using Visual Studio, these example files should automatically be moved into the build location automatically, allowing the application to use them.