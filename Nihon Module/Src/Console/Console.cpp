#pragma once
#include "Console.hpp"

Console* Console::singleton = nullptr;

auto Console::getSingleton() -> Console* {
	if (!singleton)
		singleton = new Console();

	return singleton;
}

auto Console::create() -> void {
	if (!Config::UseConsole)
		return;

	AllocConsole();
	SetConsoleTitleA((Config::Name + ' ' + Config::Version).c_str());

	freopen_s(&this->fileStream, "CONIN$", "r", stdin);
	freopen_s(&this->fileStream, "CONOUT$", "w", stdout);
	freopen_s(&this->fileStream, "CONOUT$", "w", stderr);

	EnableMenuItem(GetSystemMenu(GetConsoleWindow(), FALSE), SC_CLOSE, MF_GRAYED);
	SetWindowPos(
		GetConsoleWindow(), HWND_TOPMOST, 0, 0, 0, 0, SWP_DRAWFRAME
		| SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW
	);

	this->outputHandle = GetStdHandle(STD_OUTPUT_HANDLE);
	SMALL_RECT Rect = { 0,0, 80, 19 };
	SetConsoleWindowInfo(this->outputHandle, TRUE, &Rect);
	ShowScrollBar(GetConsoleWindow(), SB_RIGHT, 0);

	std::string logo = R"(
   _   _ _ _                 
  | \ | (_) |                
  |  \| |_| |__   ___  _ __  
  | . ` | | '_ \ / _ \| '_ \ 
  | |\  | | | | | (_) | | | |
  \_| \_/_|_| |_|\___/|_| |_|                              
    )";

	setColor(Color::Red);
	std::printf("%s\n", logo);
	setColor(Color::White);
}

auto Console::free() -> void {
	if (!Config::UseConsole)
		return;

	fclose(this->fileStream);
	FreeConsole();
}

auto Console::setColor(Color Color) -> void {
	SetConsoleTextAttribute(this->outputHandle, Color);
}