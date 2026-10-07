"""Visible ordering contracts and exact old-layout/route negative controls; not rendering."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Scripts/UI'
goal=(r/'GameUI.ProgressionGoal.cs').read_text();route=(r/'GameUI.RouteNextStep.cs').read_text();base=(r/'GameUI.cs').read_text();comparison=(r/'GameUI.EquipmentComparison.cs').read_text();skills=(r/'GameUI.MobileSkills.cs').read_text()
def fixed(source):
 surface=source[source.index('private bool DrawProgressionGoalSurface()'):source.index('private float DrawProgressionGoalOptions')]
 options=source[source.index('private float DrawProgressionGoalOptions'):]
 return '"progression-goal-current"' in surface and surface.index('current.ActionLabel')<surface.index('"progression-goals"') and 'ProgressionGoalStatus' not in options and 'current.ActionLabel' not in options
assert fixed(goal),'current progress and action must be outside candidate scroll'
assert not fixed(goal.replace('"progression-goal-current"','"progression-goals"')),'old single-scroll header mutation rejected'
assert 'case CampRouteAction.Skill:OpenRouteSkill(route.NextSkill);break;' in route
assert 'RouteSkillReturnAvailable?"返回职业路线"' in skills
grid=(r/'GameUI.InventoryGrid.cs').read_text()
assert 'EquipmentComparisonPresentation.Description(candidate' in grid and 'EquipmentComparisonPresentation.Changes(current,candidate' in grid,'inline comparison must retain mechanic benefits and costs'
assert all(t in comparison for t in ['将失去','收益：','代价：','MechanicBadgePresentation.Benefit','MechanicBadgePresentation.Cost'])
assert 'BeginTouchScroll(' not in comparison,'short comparison cannot hide tradeoffs in a 39px scroller'
assert 'CalcHeight(' in comparison and 'size=10' in comparison,'long compact summaries measured before selecting smaller typography'
print('PASS: fixed goal header, direct route detail and visible mechanism contracts with old-scroll negative control')
