-------------------------------------------------------
-- Project Porcupine Copyright(C) 2016 Team Porcupine
-- This program comes with ABSOLUTELY NO WARRANTY; This is free software,
-- and you are welcome to redistribute it under certain conditions; See
-- file LICENSE, which is part of this source code package, for details.
-------------------------------------------------------
function InitDevMode( object )
	object.ApplySettingHandler.add(DevModeSettingChanger)
	object.CancelSettingHandler.add(DevModeSettingChanger)
end

function DevModeSettingChanger()
	if (GameController.Instance.CurrentSystem ~= nil and GameController.Instance.CurrentSystem.SpawnInventoryController ~= nil) then
		GameController.Instance.CurrentSystem.SpawnInventoryController.SetUIVisibility(Settings.GetSetting("developer_general_developerMode") == "True");
	end
end