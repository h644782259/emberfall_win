"""Replay actual binding-to-slot mapping; persistence is an explicit fixture boundary."""
def include_mobile_binding_sources(output, root):
    def member(signature):
        source=(root/'Assets/Scripts/UI/GameUI.ControlPreferences.cs').read_text()
        start=source.index(signature);end=source.index('{',start)+1;depth=1
        while depth:
            depth+=(source[end]=='{')-(source[end]=='}');end+=1
        return source[start:end]
    (output/'MobileSkillPolicy.cs').write_text((root/'Assets/Scripts/Combat/MobileSkillPolicy.cs').read_text())
    (output/'BindingMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class GameUI{int[] mobileBindings=MobileSkillPolicy.DefaultBindings();void EnsureMobileBindings(){}'+member('private int BoundMobileSkill(')+member('public MobileControlLayout.Area MobileOpportunityArea(')+'}}')
