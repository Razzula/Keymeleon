# keymeleon-console
A console version of the C++ keyboard control code that compiles as an `.exe` as opposed to the main branch's `.dll`, allowing easier testing of functions.

### Prerequisites
[hidapi](https://github.com/libusb/hidapi) is required. Static library builds for both `x86` and `x64` are included and can be found in:
```
\keymeleon-console\dependencies\hidapi
```
Offical (dynamic) releases can be found [here](https://github.com/libusb/hidapi/releases).

## Acknowledgments

- [dokutan](https://github.com/dokutan)'s [rgb_keyboard](https://github.com/dokutan/rgb_keyboard) project, making it a lot easier to understand the data signals used to control the device