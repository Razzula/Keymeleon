// keymeleon-console.cpp : This file contains the 'main' function. Program execution begins and ends there.

#include <iostream>
#include <hidapi.h>

#include "keymeleon-console.h"

int main()
{
	// Initialize the hidapi library
	int res = hid_init();

	std::string hid_path;

	struct hid_device_info* devs, * cur_dev;
	devs = hid_enumerate(0x0c45, 0x652f);
	cur_dev = devs;
	//cycle through all detected HIDs
	while (cur_dev) {

		if (cur_dev->usage == 0x92) {
			hid_path = cur_dev->path;
		}

		cur_dev = cur_dev->next;
	}
	hid_free_enumeration(devs);

	if (hid_path.empty()) {
		printf("Could not find device.");
		return 1;
	}

	hid_device* handle;
	// Open the device using the path
	handle = hid_open_path(hid_path.c_str());
	if (!handle) {
		printf("unable to open device\n");
		return 1;
	}

	// ---
	// prepare data packets
	//uint8_t buf[64];
	//std::copy(std::begin(data_profile), std::end(data_profile), std::begin(buf));

	//int profile;
	//std::cin >> profile;
	//// change data
	//if (profile == 1) {
	//	//do nothing
	//}
	//else if (profile == 2) {
	//	buf[1] = 0xe1;
	//	buf[18] = 0x01;
	//}
	//else if (profile == 3) {
	//	buf[1] = 0xe2;
	//	buf[18] = 0x02;
	//}
	//else {
	//	std::cout << "Invalid input.\n";
	//	return 1;
	//}

	//res = hid_write(handle, buf, 64);
	//if (res < 0) {
	//	printf("Unable to write()\n");
	//	printf("Error: %ls\n", hid_error(handle));
	//}
	//else {
	//	printf(":D");
	//}
	// ---
	uint8_t buf[64];
	std::copy(std::begin(data_settings), std::end(data_settings),	std::begin(buf));

	res = hid_write(handle, data_start, 64);

	// keycode
	buf[1] = 0x57;
	buf[5] = 0x03;
	buf[6] = 0x00;
	// color
	buf[8] = 0xff;
	buf[9] = 0x00;
	buf[10] = 0x00;

	res = hid_write(handle, buf, 64);

	res = hid_write(handle, data_end, 64);
	// ---

	// Close the device
	hid_close(handle);

	// Finalize the hidapi library
	res = hid_exit();
	return 0;
}