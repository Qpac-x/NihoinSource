#pragma once
#include "Entry.hpp"

Expose auto Core::CreateInternal(const char* Name) -> bool {
	return true;
}

auto Core::Init(std::string ModuleName) -> void {
	auto console{ Console::getSingleton() };
	console->create();
	
	auto taskScheduler{ TaskScheduler::getSingleton() };
	taskScheduler->Initialize();

	auto L = taskScheduler->luaState;
	
	taskScheduler->setIdentity(8);
	luaL_sandboxthread(L);
	
	auto execution{ Execution::getSingleton() };
	execution->setupHook();

	auto environment{ Environment::getSingleton() };
	environment->Register(L);

	auto bridge{ Bridge::getSingleton() };
	std::thread(bridge->createPipe, Config::PipeName).detach();

	execution->executeScript(initScript);

	if (Config::UseConsole)
		MessageBoxA(nullptr, "This is a developer build, issues may occur.", (Config::Name + ' ' + Config::Version).c_str(), 0);
}

auto WINAPI DllMain(HMODULE hModule, std::uintptr_t dwReason, void*) -> int {
	DisableThreadLibraryCalls(hModule);

	if (dwReason == DLL_PROCESS_ATTACH)
		std::thread(Core::Init, Config::Name).detach();

	return 1;
}