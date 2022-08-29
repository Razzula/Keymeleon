//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

// kym.h - Contains declarations of Keymeleon's cpp library functions
#pragma once
#include <fstream>
#include <string>
#include <map>
#include <array>
#include <vector>
#include <hidapi.h>

#ifdef KYM_EXPORTS
#define KYM_TEST_API __declspec(dllexport)
#else
#define KYM_TEST_API __declspec(dllimport)
#endif

extern "C" KYM_TEST_API int SetKeyColour(char* keycode, int r, int g, int b, int profile);
extern "C" KYM_TEST_API int SetLayoutBase(char* configFileName, int profileToModify);
extern "C" KYM_TEST_API int ApplyLayoutLayer(char* configFileName, int profileToModify);
extern "C" KYM_TEST_API int SetActiveProfile(int profile);
extern "C" KYM_TEST_API int SetMode(int mode);
extern "C" KYM_TEST_API int SetPrimaryColour(int r, int g, int b);

extern std::map<std::string, std::array<uint8_t, 3>> map_keycodes;

extern uint8_t data_profile[64];
extern uint8_t data_start[64];
extern uint8_t data_end[64];
extern uint8_t data_key[64];
extern uint8_t data_row[64];
extern uint8_t data_mode[64];

hid_device* openKeyboard();
int writeToKeyboard(hid_device* handle, uint8_t buf[], int length);
std::vector<std::pair<std::string, std::array<uint8_t, 3>>> readConfigFromFile(char* filename);