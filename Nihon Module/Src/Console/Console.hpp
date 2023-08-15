#pragma once

/* <-- Core Includes --> */
#include <Windows.h>
#include <iostream>
#include <format>

/* <-- Third Party Includes --> */
#include <Config/Config.hpp>

enum Color : std::uint8_t {
    Black = 0,
    DarkBlue = 1,
    DarkGreen = 2,
    LightBlue = 3,
    DarkRed = 4,
    Magenta = 5,
    Orange = 6,
    LightGray = 7,
    Gray = 8,
    Blue = 9,
    Green = 10,
    Cyan = 11,
    Red = 12,
    Pink = 13,
    Yellow = 14,
    White = 15
};

class Console {
public:
	static Console* singleton;
	std::FILE* fileStream;
	HANDLE outputHandle;
public:
	static auto getSingleton() -> Console*;
	auto create() -> void;
	auto free() -> void;
	auto setColor(Color Color) -> void;
};