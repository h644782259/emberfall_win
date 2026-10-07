& "$PSScriptRoot/Run-CompileCheck.ps1"
foreach($level in 1..100){$expected=if($level -lt 10){1}else{[int][Math]::Floor($level/10)*10};if([Emberfall.ProgressionService]::EquipmentGenerationLevel($level)-ne $expected){throw "Gear tier failed: $level"}}
foreach($boss in @($false,$true)){foreach($roll in 0..99){if([Emberfall.TierRewardRules]::DropRarity($boss,0,$roll)-ne [Emberfall.Rarity]::Common){throw 'Non-dungeon high rarity drop'}}}
$method=[Emberfall.ProgressionService].GetMethod('SetRolledStats',[Reflection.BindingFlags]'Static,NonPublic')
foreach($slot in [Enum]::GetValues([Emberfall.ItemSlot])){
 $previous=0
 foreach($rarity in [Enum]::GetValues([Emberfall.Rarity])){
  $item=[Emberfall.ItemData]::new();$item.level=50;$item.slot=$slot;$item.rarity=$rarity;$method.Invoke($null,@($item))|Out-Null
  $score=[Emberfall.ProgressionService]::EquipmentScore($item)
  if($previous -gt 0 -and $score/$previous -lt 1.48){throw 'Rarity gap too small'}
  $previous=$score
 }
}
Write-Output 'PASS: 100 equipment tier boundaries; 200 non-dungeon rolls; rarity gaps across all slots.'
