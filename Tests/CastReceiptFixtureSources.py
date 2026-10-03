"""Add the pure production receipt dependency to explicit-source fixture projects."""
from pathlib import Path
from xml.sax.saxutils import quoteattr

def include_cast_receipt_source(project):
    project = Path(project)
    source = Path(__file__).resolve().parents[1] / 'Assets/Scripts/Core/CastFirstHitReceipt.cs'
    text = project.read_text()
    if 'CastFirstHitReceipt.cs' in text or (project.parent / 'CastFirstHitReceipt.cs').exists():
        return
    item = '<ItemGroup><Compile Include=' + quoteattr(str(source)) + ' /></ItemGroup>'
    project.write_text(text.replace('</Project>', item + '</Project>'))
