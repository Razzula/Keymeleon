<p align="center">
	<img width="256" height="256" src="https://github.com/Razzula/keymeleon/blob/main/Application/Resources/Keymeleon.svg">
</p>
<h1 align="center">Keymeleon</h1>

An adaptive keyboard RGB controller that changes the keyboard's appearance to display user-defined layouts depending on both which software is in focus and user keypresses. 

**This software is not official software and therefore is not supported by the manufacturer nor the hardware in any way. As the software interacts directly with the hardware using reverse engineered protocols, there is a risk of damage. Though the software has been used and tested, these methods may not have been extensive. No warranty is provided, especially for hardware damages, for using this software**

No irreversible damage has occurred during development or testing, but discretion is still advised.

## Overview

-  `Application\` contains the C# WPF Application Project. This is the main project.

-  `kym-Library\` contains a C++ Dynamic-link Library Project which is used by the main application. **The application will not function without this**.

-  `examples\` contains some example layout files.

### Supported Keyboards

- Tecware Phantom RGB ISO 105-key `0x652f`

In theory the software should work with other keyboard with VID `0x0c45`, but has not been tested on any beyond the above list.

_If your device is not listed, but works with the software, please do contact me to help expand the list._

## Installing

### Prerequisites

[hidapi](https://github.com/libusb/hidapi) is required. Static library builds for both `x86` and `x64` are included and can be found in:

```
\kym-Library\dependencies\hidapi
```

Official (dynamic) releases can be found [here](https://github.com/libusb/hidapi/releases).

### Installing

Both the application and C++ library are projects under a single Visual Studio solution and the easiest method is to build using VS, as it should handle the whole process for you.

If however it fails, or should you wish to build the project independently, the steps to install are:

- Ensure the prerequisites are met

- Build `kym.dll` from `\kym-Library`

- Build `Keymeleon.exe` from `\Application`

- Move `kym.dll` into the build location of the application.

Optional (but advised):

- Move `examples\Default.base` (and any `.conf` files you wish) into `BUILD_LOCATION\layouts`

The application will only work on Windows devices. However, the C++ Library should work on other platforms, provided the correct version of hidapi is present.

## Usage

Run the executable to start the program. When running for the first time, the editor and settings menu will open, allowing you to enable startup, etc. To use the editor, simply select the layer you wish to edit in the top left (base, application, hotkey), select the colour using the tool in the top right, and click on the key buttons to colour them. (When creating a new layer, the editor will only allow you to select applications which are currently open.)

Upon closing the editor, or on subsequent launches, the program will automatically detect the keyboard and begin responding to window changes as described by any `.conf` files in the application's folder.

You can reopen the editor via the system tray icon (double or right click).

### Notes

- Running an `x86` build on a `x64` machine can lead to unusual application names (i.e. `Google Chrome` will just be seen as `chrome`, and `Microsoft Visual Studio 2022` will just be `devenv`)
- The same is true if trying to monitor an elevated application without Keymeleon being elevated

## License
### GNU GPLv3
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

See [LICENSE.md](LICENSE.md) for details.

## Acknowledgments

- [dokutan](https://github.com/dokutan)'s [rgb_keyboard](https://github.com/dokutan/rgb_keyboard) project, making it a lot easier to understand the data signals used to control the device

- Microsoft's [Visual Studio Image Library 2022](https://www.microsoft.com/en-gb/download/details.aspx?id=35825)