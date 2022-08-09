# Keymeleon
An adaptive keyboard RGB controller that changes the keyboard's apperance depending on which software is in focus.

## Overview
- `Application\` contains the C# WPF Application Project. This is the main project.
- `kym-Library\` contains a C++ Dynamic-link Library Project which is used by the main application. **The application will not function without this**.
- `examples\` contains some example `.conf` files.

## Prerequisites
[hidapi](https://github.com/libusb/hidapi) is required. Static library builds for both `x86` and `x64` are included and can be found in:
```
\kym-Library\dependencies\hidapi
```
Offical (dynamic) releases can be found [here](https://github.com/libusb/hidapi/releases).

## Acknowledgments

- [dokutan](https://github.com/dokutan)'s [rgb_keyboard](https://github.com/dokutan/rgb_keyboard) project, making it a lot easier to understand the data signals used to control the device