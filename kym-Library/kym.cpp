// kym.cpp : Defines the exported functions for the DLL.

#include <fstream>
#include <Windows.h>

#include "pch.h"
#include "kym.h"

hid_device* openKeyboard() {
	std::string hid_path;

	hid_init();

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

int writeToKeyboard(hid_device* handle, uint8_t buf[], int length) {

	//write buf data to device at handle
	int res = hid_write(handle, buf, length);
	if (res < 0) {
		printf("Unable to write\n");
		printf("Error: %ls\n", hid_error(handle));
	}

	hid_read_timeout(handle, buf, 64, 500); // this appears to have solved the errors caused in commit 2421730d3cb37a873360a470f1418cd1e67dcedd (6c332890fac98778751cff7a3ae36b25e47d32a9)
	return res;
}

std::vector<std::pair<std::string, std::array<uint8_t, 3>>> readConfigFromFile(char* filename) {
	std::vector<std::pair<std::string, std::array<uint8_t, 3>>> layout;

	// open file
	std::ifstream config(filename);
	std::istream& configRef = config;

	// read file
	if (config.is_open()) { // always check whether the file is open
		while (config.good()) {
			std::string line;
			std::string keycode = "";

			std::getline(configRef, line); //read line of file

			int charOfLine = 0;
			if (line[0] == '#') { //ignore comments
				continue;
			}
			//extract keycode from line
			for (charOfLine; charOfLine < line.length(); charOfLine++) {
				char c = line[charOfLine];
				if (c == '\t') {
					break;
				}
				keycode += c;
			}

			//split remainder of line into 3 seperate hex values for rgb
			std::array<uint8_t, 3> colour;
			for (int valueOfColour = 0; valueOfColour < 3; valueOfColour++) {
				char subvalue[2];

				if (charOfLine + 2 < line.length()) {
					subvalue[0] = line[charOfLine += 1];
					subvalue[1] = line[charOfLine += 1];

				}
				else {
					colour = { 0xff, 0x00, 0x00 };
					break;
				}

				colour[valueOfColour] = (uint8_t)strtol(subvalue, nullptr, 16); //converts string hex to numerical
			}

			//store in vector
			layout.push_back(std::make_pair(keycode, colour));

		}
	}
	config.close();

	return layout;
}

int SetKeyColour(char* keycode, int r, int g, int b, int profile) { //TODO; refactor! a lot of duplicate code between this and setCustomLayout()
	profile -= 1;
	int res = 0;

	hid_device* handle = openKeyboard();
	if (!handle) {
		return -1;
	}

	res += writeToKeyboard(handle, data_start, 64); //tell device this is start of data

	std::array<uint8_t, 3> keyID;
	try {
		keyID = map_keycodes.at(keycode);
	}
	catch (std::out_of_range) {
		return -1;
	}

	uint8_t buf[64];
	std::copy(std::begin(data_settings), std::end(data_settings), std::begin(buf));
	// set keycode values
	buf[1] = keyID[0] + 2 * profile;
	buf[5] = keyID[1];
	buf[6] = keyID[2] + 2 * profile;
	// set colour values
	buf[8] = r;
	buf[9] = g;
	buf[10] = b;

	// write key config to device
	res += writeToKeyboard(handle, buf, 64);

	//std::cout << keycode << " " << r << " " << g << " " << b << std::endl;

	//Sleep(500); //slight delay, to ensure data tranmisisons have finished
	res += writeToKeyboard(handle, data_end, 64); //tell device this end of data

	return res;

}

int ApplyLayoutLayer(char* configFileName, int profileToModify) {
	auto layout = readConfigFromFile(configFileName); //get data from config file
	//return layout.size();

	profileToModify -= 1;
	int res = 0;

	hid_device* handle = openKeyboard();
	if (!handle) {
		return res;
	}

	res += writeToKeyboard(handle, data_start, 64); //tell device this is start of data

	uint8_t buf[64];
	std::copy(std::begin(data_settings), std::end(data_settings), std::begin(buf));

	for (auto element : layout) { //for every key config in layout
		// search keycode map for key identifier
		std::array<uint8_t, 3> keyID;
		try {
			keyID = map_keycodes.at(element.first);
		}
		catch (std::out_of_range) {
			//std::cout << "Invalid keycode: " << element.first << std::endl;
			continue;
		}

		// set keycode values
		buf[1] = keyID[0] + 2 * profileToModify;
		buf[5] = keyID[1];
		buf[6] = keyID[2] + 2 * profileToModify;
		// set colour values
		buf[8] = element.second[0];
		buf[9] = element.second[1];
		buf[10] = element.second[2];

		// write key config to device
		////Sleep(10);
		int tempRes = writeToKeyboard(handle, buf, 64);
		if (tempRes == -1) {
			hid_close(handle);
			hid_exit();
			return res - 1;
		}
		res += tempRes;
		//std::cout << element.first << " " << unsigned(element.second[0]) << " " << unsigned(element.second[1]) << " " << unsigned(element.second[2]) << std::endl;//DEBUG
	}
	////Sleep(250); //slight delay, to ensure data tranmisisons have finished
	res += writeToKeyboard(handle, data_end, 64); //tell device this end of data
	////Sleep(250);

	hid_close(handle);
	hid_exit();
	return res;
}

int SetActiveProfile(int profile) {

	int res;
	hid_device* handle = openKeyboard();
	if (!handle) {
		return -1;
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
		//std::cout << "Invalid input.\n";
		return 1;
	}

	res = writeToKeyboard(handle, buf, 64);

	hid_close(handle);
	hid_exit();
	return res;
}

int SetLayoutBase(char* fileName, int profile) {

	profile--;

	int res = 0;
	hid_device* handle = openKeyboard();
	if (!handle) {
		return -1;
	}

	int rowHeaders[] = { 3, 54, 105, 156, 207, 2, 59 };
	int rowHeaderPtr = 0;

	// open file
	std::ifstream config(fileName);
	std::istream& configRef = config;

	res += writeToKeyboard(handle, data_start, 64); //tell device this is start of data

	// read file
	if (config.is_open()) { // always check whether the file is open

		while (config.good()) {
			std::string line;
			std::string colourCode = "";
			std::vector<uint8_t> colourCodes; //TODO remove vector and store data directly in buf

			std::getline(configRef, line); //read line of file

			int charOfLine = 0;
			if (line[0] == '#') { //ignore comments
				continue;
			}

			//extract keycode from line
			for (charOfLine; charOfLine < line.length(); charOfLine++) {
				char c = line[charOfLine];

				if (c != ' ') {
					colourCode += c;
				}
				else {
					//split colourCode into 3 seperate hex values for rgb
					std::array<uint8_t, 3> colour;
					for (int valueOfColour = 0; valueOfColour < 3; valueOfColour++) {
						char subvalue[2];
						subvalue[0] = line[charOfLine += 1];
						subvalue[1] = line[charOfLine += 1];

						uint8_t colour = (uint8_t)strtol(subvalue, nullptr, 16); //converts string hex to numerical
						//store in vector
						colourCodes.push_back(colour);
					}
				}
			}

			uint8_t buf[64];
			std::copy(std::begin(data_row), std::end(data_row), std::begin(buf)); //get data signal for switch profile

			buf[5] = rowHeaders[rowHeaderPtr]; //start of row
			if (rowHeaderPtr < 5) {
				buf[6] = profile * 2; //profile
			}
			else {
				buf[6] = profile * 2 + 1; //profile overflow
			}

			//fill buf
			for (int i = 0; i < colourCodes.size(); i++) {
				buf[8 + i] = colourCodes[i];
			}

			res += writeToKeyboard(handle, buf, 64);

			rowHeaderPtr += 1;
		}
	}
	config.close();

	res += writeToKeyboard(handle, data_end, 64); //tell device this is end of data

	return res;
}