# Keymeleon
An adaptive keyboard RGB controller that changes the keyboard's appearance to display user-defined layouts depending on which software is in focus.

**This software is not official software and therefore is not supported by the manufacturer nor the hardware in any way. Though the software has been used and tested, these methods may not have been extensive. No warranty is provided, especially for hardware damages, for using this software**

## Overview
- `Application\` contains the C# WPF Application Project. This is the main project.
- `kym-Library\` contains a C++ Dynamic-link Library Project which is used by the main application. **The application will not function without this**.
- `examples\` contains some example layout files.

### Supported Keyboards
- Tecware Phantom RGB ISO 105-key `0x652f`

In theory the software should work with other keyboard with VID  `0x0c45`, but has not been tested on any beyond the above list.

_If your device is not listed, but works with the software, please do contact me to help expand the list._

## Installing
### Prerequisites
[hidapi](https://github.com/libusb/hidapi) is required. Static library builds for both `x86` and `x64` are included and can be found in:
```
\kym-Library\dependencies\hidapi
```
Offical (dynamic) releases can be found [here](https://github.com/libusb/hidapi/releases).

### Installing
Both the application and C++ library are projects under a single Visual Studio solution and the easiest method is to build using VS, as it should handle the whole process for you.

If however it fails, or should you wish to build the project independently, the steps to install are:
- Ensure the prerequisites are met
- Build `kym.dll` from `\kym-Library`
- Build `Keymeleon.exe` from `\Application`
- Move `kym.dll` into the build location of the application
- Move `examples\default.base` into `BUILD_LOCATION\layouts`

The application will only work on Windows devices. However, the C++ Library should work on other platforms, provided the correct version of hidapi is present.

## Usage
Run the executable to start the program. The program will automatically detect the keyboard and begin responding to window changes as described by any `.conf` files in the application's folder.

To create or edit these files, open the editor via the system tray icon, set the desired layout, and save the file as `NAME.conf`, where `NAME` is the (*case sensitive*) name of the application this layout should be used for.

## Acknowledgments

- [dokutan](https://github.com/dokutan)'s [rgb_keyboard](https://github.com/dokutan/rgb_keyboard) project, making it a lot easier to understand the data signals used to control the device
- Microsoft's [Visual Studio Image Library 2022](https://www.microsoft.com/en-gb/download/details.aspx?id=35825)