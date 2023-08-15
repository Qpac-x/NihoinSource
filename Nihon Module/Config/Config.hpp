#pragma once

/* <-- Core Includes --> */
#include <string>
#include <format>

namespace Config {
	/* <-- Module Info --> */
	const std::string Name { "Nihon" };
	const std::string Version { "2.581.563.0" };

	/* <-- File System --> */
	const std::string WorkspacePath { std::format("{}\\Workspace", std::getenv("localappdata")) };
	const std::string AutoExecutePath { std::format("{}\\AutoExec", std::getenv("localappdata")) };

	/* <-- Debugging --> */
	const bool DebugMode { true };
	const bool RunOnScheduler{ true }; // Switch between scheduler and pushing a closure to heartbeat (not ready yet).
	const bool UseConsole{ true };

	/* <-- Pipe Setting --> */
	const std::string PipeName { "Nihon-Pipe" };
	const std::string InteractName { "Nihon-Interact" };

	/* <-- Socket Setting --> */
	const std::string Host { "127.0.0.1" };
	const std::intptr_t Port { 4200 };
}