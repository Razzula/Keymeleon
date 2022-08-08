#pragma once
#include <iostream>
#include <string>
#include <map>
#include <array>
#include <hidapi.h>

extern std::map<std::string, std::array<uint8_t, 3>> map_keycodes;

extern uint8_t data_profile[64];
extern uint8_t data_start[64];
extern uint8_t data_end[64];
extern uint8_t data_settings[64];

hid_device* openKeyboard();
int writeToKeyboard(hid_device* handle, uint8_t buf[], int length);
void setCustomLayout(std::array<std::pair<std::string, std::array<uint8_t, 3>>, 1> layout);
int setActiveProfile(int profile);