from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(name): return (root/'Assets/Scripts'/name).read_text()
p=read('Combat/PlayerController.cs')
a=read('Combat/PlayerController.SkillAvailability.cs')
t=read('Combat/SkillTargetingController.cs')
s=read('Combat/SummonerSpell.cs')
g=read('Core/GameTypes.cs')
assert 'HeroClass==HeroClass.Vanguard && slot == 6' in p
assert 'HeroClass==HeroClass.Vanguard&&skill==6' in a
assert 'slot==6&&HeroClass==HeroClass.Arcanist' in p and 'Damage(2.5f*power)' in p
assert 'slot==6&&HeroClass==HeroClass.Ranger' in p and 'i*8' in p
assert 'damage*2.6f' in s and 'damage*3.2f' in s
assert 'AdvancedSkillSequence.Spawn' not in s
assert 'index == 6' in t
assert 'guardReduction=0;guardRadius' in p
assert 'if(HeroClass==HeroClass.Vanguard)AdvancedSkillVfx.Protection' in p
assert 'Form==Kind.Treant' in read('Combat/SummonedCompanion.cs')
assert 'healingPulseTime>=3f' in read('Combat/SummonedCompanion.cs')
assert 'Owner.Heal(Owner.MaxHealth*.06f)' in read('Combat/SummonedCompanion.cs')
for old in ['灵魂护盾','回春共鸣','森林祈愿','法力屏障','减伤30%但机动']: assert old not in g
assert 'hero == HeroClass.Vanguard ? SkillCategory.Healing : SkillCategory.Damage' in g
print('PASS class attack dispatch, healing gates, shield ownership, targeting, pet healing and descriptions source guards')
