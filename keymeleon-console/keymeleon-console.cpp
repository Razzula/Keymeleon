// keymeleon-console.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <hidapi.h>

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
		printf("Device Found\n  type: %04hx %04hx\n  path: %s\n  serial_number: %ls", cur_dev->vendor_id, cur_dev->product_id, cur_dev->path, cur_dev->serial_number);
		printf("\n");
		printf("  Manufacturer: %ls\n", cur_dev->manufacturer_string);
		printf("  Product:      %ls\n", cur_dev->product_string);
		printf("  Release:      %hx\n", cur_dev->release_number);
		printf("  Interface:    %d\n", cur_dev->interface_number);
		printf("  Usage (page): 0x%hx (0x%hx)\n", cur_dev->usage, cur_dev->usage_page);
		printf("\n");

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

	/*
		...
	*/

	// Close the device
	hid_close(handle);

	// Finalize the hidapi library
	res = hid_exit();
	return 0;
}