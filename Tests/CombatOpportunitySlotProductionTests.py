#!/usr/bin/env python3
"""H05 extends the former slot/basic recorder with the actual shared outer meter."""
import subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
subprocess.run([sys.executable,str(root/'Tests/MobileOpportunityPresentationTests.py'),*sys.argv[1:]],check=True)
subprocess.run([sys.executable,str(root/'Tests/CombatOpportunitySourceTests.py')],check=True)
