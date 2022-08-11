#pragma once
#include <fstream>
#include <string>
#include <map>
#include <array>
#include <vector>
#include <hidapi.h>

extern int setKeyColour(char* keycode, int r, int g, int b, int profile);
extern int setCustomLayout(char* configFileName, int profileToModify);
extern int setActiveProfile(int profile);

extern std::map<std::string, std::array<uint8_t, 3>> map_keycodes;

extern uint8_t data_profile[64];
extern uint8_t data_start[64];
extern uint8_t data_end[64];
extern uint8_t data_settings[64];
extern uint8_t data_test[64];

hid_device* openKeyboard();
int writeToKeyboard(hid_device* handle, uint8_t buf[], int length);
std::vector<std::pair<std::string, std::array<uint8_t, 3>>> readConfigFromFile(char* filename);
void test();