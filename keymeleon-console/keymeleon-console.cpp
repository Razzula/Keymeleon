// keymeleon-console.cpp : This file contains the 'main' function. Program execution begins and ends there.

#include <iostream>
#include <hidapi.h>

#include "keymeleon-console.h"

int main()
{
	// Initialize the hidapi library
	hid_init();

	// this small section shows that inactive profiles can be altered
	char a;

	setActiveProfile(3);
	std::cin >> a;
	
	std::array<uint8_t, 3> colours = {0xff, 0x00, 0x00};
	std::array<std::pair<std::string, std::array<uint8_t, 3>>, 1> layout = { std::make_pair("Esc", colours) };
	setCustomLayout(layout);
	std::cin >> a;

	setActiveProfile(1);

	// Finalize the hidapi library
	hid_exit();
	return 0;
}

hid_device* openKeyboard() {
	std::string hid_path;

	struct hid_device_info* devs, * cur_dev;
	devs = hid_enumerate(0x0c45, 0x652f); //scan for devices matching VID and PID
	cur_dev = devs;
	//cycle through all detected HIDs
	while (cur_dev) {

		if (cur_dev->usage == 0x92) { //RGB control interface
			hid_path = cur_dev->path;
		}

		cur_dev = cur_dev->next;
	}
	hid_free_enumeration(devs);

	if (hid_path.empty()) {
		printf("Could not find device.\n");
		return nullptr;
	}

	// Open the device using the path
	hid_device* handle;
	handle = hid_open_path(hid_path.c_str());
	if (!handle) {
		printf("Unable to open device.\n");
		return nullptr;
	}

	return handle;
}

int writeToKeyboard(hid_device *handle, uint8_t buf[], int length) {

	//write buf data to device at handle
	int res = hid_write(handle, buf, length);
	if (res < 0) {
		printf("Unable to write\n");
		printf("Error: %ls\n", hid_error(handle));
	}

	return res;
}

void setCustomLayout(std::array<std::pair<std::string, std::array<uint8_t, 3>>, 1> layout) {
	int res;

	hid_device* handle = openKeyboard();
	if (!handle) {
		return;
	}

	uint8_t buf[64];
	std::copy(std::begin(data_settings), std::end(data_settings), std::begin(buf));

	res = writeToKeyboard(handle, data_start, 64); //tell device this is start of data

	for (auto element :layout) { //for every key config in layout
		// search keycode map for key identifier
		auto keyID = map_keycodes.at(element.first);
		// set keycode values
		buf[1] = keyID[0];
		buf[5] = keyID[1];
		buf[6] = keyID[2];
		// set colour values
		buf[8] = element.second[0];
		buf[9] = element.second[1];
		buf[10] = element.second[2];

		// write key config to device
		res = writeToKeyboard(handle, buf, 64);
	}

	res = writeToKeyboard(handle, data_end, 64); //tell device this end start of data

	hid_close(handle);
}

int setActiveProfile(int profile) {

	int res;
	hid_device* handle = openKeyboard();
	if (!handle) {
		return 1;
	}

	uint8_t buf[64];
	std::copy(std::begin(data_profile), std::end(data_profile), std::begin(buf)); //get data signal for switch profile

	//change signal to correspond to correct profile
	if (profile == 1) {
		//do nothing, 1 is default of data_profile
	}
	else if (profile == 2) {
		buf[1] = 0xe1;
		buf[18] = 0x01;
	}
	else if (profile == 3) {
		buf[1] = 0xe2;
		buf[18] = 0x02;
	}
	else {
		std::cout << "Invalid input.\n";
		return 1;
	}

	res = writeToKeyboard(handle, buf, 64);

	hid_close(handle);
	return res;
}