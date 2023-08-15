#pragma once

/* <-- Core Includes --> */
#include <Windows.h>
#include <thread>

/* <-- Third Party Includes --> */
#include <Config/Config.hpp>
#include <Console/Console.hpp>
#include <Exploit/Bridge.hpp>
#include <Exploit/Exploit-State/TaskScheduler.hpp>
#include <Exploit/Execution/Execution.hpp>
#include <Exploit/Environment/Environment.hpp>

/* <-- Defines --> */
#define Expose __declspec(dllexport)

std::string initScript = R"(
		getgenv().require = newcclosure(function(module)
			local identity = getthreadidentity()
			setthreadidentity(2)

			local data = { pcall(o_require, module) }
			setthreadidentity(identity)

			if (data[1] == false) then 
				error(data[2], 0)
				return nil
			end
	
			return data[2]
		end)
		getgenv().getscripthash = function(Script)
			return Script:GetHash()
		end

		getgenv().getinstances = function()
			local Table = {}
			for i, v in next, getreg() do
				if type(v) == "table" then
					for n, c in next, v do
						if typeof(c) == "Instance" then
							table.insert(Table, c)
						end
					end
				end
			end
			return Table
		end

		getgenv().getnilinstances = function()
			local Ret = {}
			for i, v  in next, getinstances() do
				if v.Parent == nil then
					Ret[#Ret + 1] = v
				end
			end
			return Ret
		end

		getgenv().getscripts = function()
			local Ret = {}
			for _, v in next, getinstances() do
				if (v:IsA("LocalScript") or v:isA("CoreScript"))  then
					Ret[#Ret + 1] = v
				end
			end
			return Ret
		end

		getgenv().getloadedmodules = function()
			local Ret = {}
			for _, v in next, getinstances() do
				if (v:IsA("ModuleScript")) then
					Ret[#Ret + 1] = v
				end
			end
			return Ret
		end

		getgenv().getscriptclosure = function(a1)
			for _, t in pairs(getreg()) do
				if type(t) == "table" then
					for _, v in pairs(t) do
						if type(v) == "function" and getfenv(v) and rawget(getfenv(v), "script") == a1 then
							return v
						end
					end
				end
			end
		end

		getgenv().isnetworkowner = function(Part)
			return (Part:GetNetworkOwner() == game:GetService("Players").LocalPlayer)
		end

		getgenv().getconnections = function(a1)
			local Temp = a1:Connect(function() end)
			local Signals = getothersignals(Temp)
			for i, v in pairs(Signals) do
				Signals[i] = setmetatable(v, MT)
			end
			Temp:Disconnect()
			return Signals
		end

		getgenv().fireproximityprompt = function(Obj, Amount, Skip)
			if Obj.ClassName == "ProximityPrompt" then 
				Amount = Amount or 1
				local PromptTime = Obj.HoldDuration
				if Skip then 
					Obj.HoldDuration = 0
				end
				for i = 1, Amount do 
					Obj:InputHoldBegin()
					if not Skip then 
						wait(Obj.HoldDuration)
					end
					Obj:InputHoldEnd()
				end
				Obj.HoldDuration = PromptTime
			else 
				error("userdata<ProximityPrompt> expected")
			end
		end

		getgenv().firesignal = function(Signal, ...)
            assert(Signal, "Signal Request")
            for _, v in pairs(getconnections(Signal)) do
				v.Function(...)
            end
        end

		getgenv().firetouchinterest = function(a1, a2, Mode)
			if Mode == 0 then
				for _, v in pairs(getconnections(a2.Touched)) do
					v.Function(a1)
				end
			end
		end

		getgenv().gethiddenproperty = newcclosure(function(inst, idx) 
			assert(typeof(script) == "Instance", "invalid argument #1 to 'gethiddenproperty' [Instance expected]", 2)
			local was = isscriptable(inst, idx)
			setscriptable(inst, idx, true)

			local value = inst[idx]
			setscriptable(inst, idx, was)

			return value, not was
		end)

		getgenv().sethiddenproperty = newcclosure(function(inst, idx, value) 
			assert(typeof(script) == "Instance", "invalid argument #1 to 'sethiddenproperty' [Instance expected]", 2)
			local was = isscriptable(inst, idx)
			setscriptable(inst, idx, true)

			inst[idx] = value

			setscriptable(inst, idx, was)

			return not was
		end)

		getgenv().getsenv = newcclosure(function(script) 
			assert(typeof(script) == "Instance", "invalid argument #1 to 'getsenv' [ModuleScript or LocalScript expected]", 2)
			assert((script:IsA("LocalScript") or script:IsA("ModuleScript")), "invalid argument #1 to 'getsenv' [ModuleScript or LocalScript expected]", 2)
			if (script:IsA("LocalScript") == true) then 
				for i,v in getreg() do
					if (type(v) == "function") then
						if getfenv(v).script then
							if getfenv(v).script == script then
								return getfenv(v)
							end
						end
					end
				end
			else
				local A = getreg()
				local B = {}

				if #A == 0 then
					return require(script)
				end

				for C, D in next, A do
					if type(D) == "function" and islclosure(D) then
						local E = getfenv(D)
						local F = rawget(E, "script")
						if F and F == script then
							for G, H in next, E do
								if G ~= "script" then
									rawset(B, G, H)
								end
							end
						end
					end
				end
				return B
			end
		end)
		getgenv().getmenv = getgenv().getsenv

		local function lookupify(t)
			local _t = {}
			for k, v in t do
				_t[v] = true
			end
			return _t
		end

		local AllowedMethods = lookupify{ "__index", "__namecall", "__newindex", "__call", "__concat", "__unm", "__add", "__sub", "__mul", "__div", "__pow", "__mod", "__tostring", "__eq", "__lt", "__le", "__gc", "__len" }
		getgenv().hookmetamethod = newcclosure(function(ud, method, fn)
			assert(ud ~= nil, 'invalid argument #1 (object expected)', 0)
			assert(typeof(method) == "string", 'invalid argument #2 (string expected)', 0)
			assert(typeof(fn) == "function", 'invalid argument #3 (function expected)', 0)
			assert(AllowedMethods[method], 'invalid argument #2 (function mode expected)', 0)

			local gmt = getrawmetatable(ud)
			local old_fn = rawget(gmt, method)

			if (old_fn == nil and type(old_fn) ~= "function") then 
				return 
			end

			setreadonly(gmt, false)
			rawset(gmt, method, (islclosure(fn) and newcclosure(fn) or fn))
			setreadonly(gmt, true)
			return old_fn
		end)

		getgenv().GetObjects = newcclosure(function(Id) 
			return { game:GetService("InsertService"):LoadLocalAsset(Id) }
		end)

		getgenv().get_nil_instances = getgenv().getnilinstances
		
		local Keywords = { "OpenScreenshotsFolder", "OpenVideosFolder", "ReportAbuse", "GetMessageId", "Publish", "OpenBrowserWindow", "ExecuteJavaScript", "GetRobuxBalance", "PerformPurchase" }
		local Metatable = getrawmetatable(game)
		local Game = game
		
		local OldMetatable = Metatable.__namecall
		
		-- Namecall
		setreadonly(Metatable, false)
		Metatable.__namecall = function(Self, ...)
		    local Method = getnamecallmethod()
		        if Method == "HttpGet" or Method == "HttpGetAsync" then
		            return HttpGet(...)
		        elseif Method == "GetObjects" then 
		            return GetObjects(...)
		        end
		    if table.find(Keywords, getnamecallmethod()) then
		        return "Attempt To Call Protected Function"
		    end
		    return OldMetatable(Self, ...)
		end
		
		local OldIndex = Metatable.__index
		
		-- Index
		setreadonly(Metatable, false)
		Metatable.__index = function(Self, i)
		    if Self == game then
		        if i == "HttpGet" or i == "HttpGetAsync" then 
		            return HttpGet
		        elseif i == "GetObjects" then 
		            return GetObjects
		        end
		    end
		    if table.find(Keywords, i) then
		        return "Attempt To Call Protected Function"
		    end
		    return OldIndex(Self, i)
		end

		loadstring(game:HttpGet("https://raw.githubusercontent.com/Nihon-Development/Nihon-Module/Entry/Injection%20Screen.lua"))()
)";

namespace Core {
	Expose auto CreateInternal(const char* Name) -> bool;
	auto Init(std::string ModuleName) -> void;
}