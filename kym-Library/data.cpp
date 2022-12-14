//  Copyright (C) 2022  Jack Gillespie  https://github.com/Razzula/Keymeleon/blob/main/LICENSE.md

// This file contains the data values used to control the keyboard.

#include "pch.h"
#include "kym.h"

std::map<std::string, std::array<uint8_t, 3>> map_keycodes = { //ISO
	{"Esc", {0x57, 0x03, 0x00}},
	{"F1", {0x5a, 0x06, 0x00}},
	{"F2", {0x5d, 0x09, 0x00}},
	{"F3", {0x60, 0x0c, 0x00}},
	{"F4", {0x63, 0x0f, 0x00}},
	{"F5", {0x66, 0x12, 0x00}},
	{"F6", {0x69, 0x15, 0x00}},
	{"F7", {0x6c, 0x18, 0x00}},
	{"F8", {0x6f, 0x1b, 0x00}},
	{"F9", {0x72, 0x1e, 0x00}},
	{"F10", {0x75, 0x21, 0x00}},
	{"F11", {0x78, 0x24, 0x00}},
	{"F12", {0x7b, 0x27, 0x00}},
	{"PrtSc", {0x93, 0x3e, 0x01}},
	{"ScrLk", {0x96, 0x41, 0x01}},
	{"Pause", {0x99, 0x44, 0x01}},
	{"Tilde", {0x8a, 0x36, 0x00}},
	{"1", {0x8d, 0x39, 0x00}},
	{"2", {0x90, 0x3c, 0x00}},
	{"3", {0x93, 0x3f, 0x00}},
	{"4", {0x96, 0x42, 0x00}},
	{"5", {0x99, 0x45, 0x00}},
	{"6", {0x9c, 0x48, 0x00}},
	{"7", {0x9f, 0x4b, 0x00}},
	{"8", {0xa2, 0x4e, 0x00}},
	{"9", {0xa5, 0x51, 0x00}},
	{"0", {0xa8, 0x54, 0x00}},
	{"Minus", {0xab, 0x57, 0x00}},
	{"Equals", {0xae, 0x5a, 0x00}},
	{"Backspace", {0x7b, 0x26, 0x01}},	//(ANSI) International Key 
	{"Insert", {0x9f, 0x4a, 0x01}},
	{"Home", {0xa2, 0x4d, 0x01}},
	{"PgUp", {0xa5, 0x50, 0x01}},
	{"Tab", {0xbd, 0x69, 0x00}},
	{"q", {0xc0, 0x6c, 0x00}},
	{"w", {0xc3, 0x6f, 0x00}},
	{"e", {0xc6, 0x72, 0x00}},
	{"r", {0xc9, 0x75, 0x00}},
	{"t", {0xcc, 0x78, 0x00}},
	{"y", {0xcf, 0x7b, 0x00}},
	{"u", {0xd2, 0x7e, 0x00}},
	{"i", {0xd5, 0x81, 0x00}},
	{"o", {0xd8, 0x84, 0x00}},
	{"p", {0xdb, 0x87, 0x00}},
	{"BracketL", {0xde, 0x8a, 0x00}},
	{"BracketR", {0xe1, 0x8d, 0x00}},
	{"Enter", {0x47, 0xf3, 0x00}},
	{"Delete", {0xa8, 0x53, 0x01}},
	{"End", {0xab, 0x56, 0x01}},
	{"PgDn", {0xae, 0x59, 0x01}},
	{"CapsLock", {0xf0, 0x9c, 0x00}},
	{"a", {0xf3, 0x9f, 0x00}},
	{"s", {0xf6, 0xa2, 0x00}},
	{"d", {0xf9, 0xa5, 0x00}},
	{"f", {0xfc, 0xa8, 0x00}},
	{"g", {0xff, 0xab, 0x00}},
	{"h", {0x02, 0xae, 0x00}},
	{"j", {0x05, 0xb1, 0x00}},
	{"k", {0x08, 0xb4, 0x00}},
	{"l", {0x0b, 0xb7, 0x00}},
	{"Semicolon", {0x0e, 0xba, 0x00}},
	{"Apostrophe", {0x11, 0xbd, 0x00}},
	{"Hash", {0x14, 0xc0, 0x00}},		//(ANSI) Backslash
	{"LShift", {0x23, 0xcf, 0x00}},
	{"Backslash", {0x4f, 0x3b, 0x01}},
	{"z", {0x26, 0xd2, 0x00}},
	{"x", {0x29, 0xd5, 0x00}},
	{"c", {0x2c, 0xd8, 0x00}},
	{"v", {0x2f, 0xdb, 0x00}},
	{"b", {0x32, 0xde, 0x00}},
	{"n", {0x35, 0xe1, 0x00}},
	{"m", {0x38, 0xe4, 0x00}},
	{"Comma", {0x3b, 0xe7, 0x00}},
	{"Period", {0x3e, 0xea, 0x00}},
	{"Slash", {0x41, 0xed, 0x00}},
	{"RShift", {0x44, 0xf0, 0x00}},
	{"Up", {0x75, 0x20, 0x01}},
	{"LCtrl", {0x57, 0x02, 0x01}},
	{"Super", {0x5a, 0x05, 0x01}},
	{"LAlt", {0x5d, 0x08, 0x01}},
	{"Space", {0x60, 0x0b, 0x01}},
	{"RAlt", {0x63, 0x0e, 0x01}},
	{"Fn", {0x66, 0x11, 0x01}},
	{"Menu", {0x69, 0x14, 0x01}},
	{"RCtrl", {0x6c, 0x17, 0x01}},
	{"Left", {0x6f, 0x1a, 0x01}},
	{"Down", {0x72, 0x1d, 0x01}},
	{"Right", {0x78, 0x23, 0x01}},

	{"Num_Lock", {0xb1, 0x5d, 0x00}},
	{"Num_Slash", {0xb4, 0x60, 0x00}},
	{"Num_Asterisk", {0xb7, 0x63, 0x00}},
	{"Num_Minus", {0xb1, 0x5c, 0x01}},
	{"Num_7", {0xe4, 0x90, 0x00}},
	{"Num_8", {0xe7, 0x93, 0x00}},
	{"Num_9", {0xea, 0x96, 0x00}},
	{"Num_Plus", {0xb4, 0x5f, 0x01}},
	{"Num_4", {0x17, 0xc3, 0x00}},
	{"Num_5", {0x1a, 0xc6, 0x00}},
	{"Num_6", {0x1d, 0xc9, 0x00}},
	{"Num_1", {0x4a, 0xf6, 0x00}},
	{"Num_2", {0x4d, 0xf9, 0x00}},
	{"Num_3", {0x50, 0xfc, 0x00}},
	{"Num_0", {0x7e, 0x29, 0x01}},
	{"Num_Period", {0x81, 0x2c, 0x01}},
	{"Num_Enter", {0x84, 0x2f, 0x01}}
};

uint8_t data_profile[] = {
	0x04, 0xe0, 0x03, 0x04, 0x2c, 0x00, 0x00, 0x00, /* ....,... */
	0x55, 0xaa, 0xff, 0x02, 0x45, 0x0c, 0x2f, 0x65, /* U...E./e */
	0x03, 0x01, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, /* ........ */
	0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x08, 0x07, /* ........ */
	0x09, 0x0b, 0x0a, 0x0c, 0x0d, 0x0f, 0x0e, 0x10, /* ........ */
	0x12, 0x11, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, /* ........ */
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /* ........ */
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00	/* ........ */
};

uint8_t data_start[] = { 0x04, 0x01, 0x00, 0x01 };
uint8_t data_end[] = { 0x04, 0x02, 0x00, 0x02 };
uint8_t data_key[] = { 0x04, 0x00, 0x02, 0x11, 0x03 };
uint8_t data_row[] = { 0x04, 0x20, 0x27, 0x11, 0x30 };
uint8_t data_mode[] = { 0x04, 0x00, 0x00, 0x06, 0x01 };