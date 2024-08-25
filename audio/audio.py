import os
import subprocess
from pathlib import Path
from sys import argv


def audio_convert(source: Path, dest: Path):
    # this relpath step is necessary because cygwin has some odd admission control on /cygdrive/d
    # using the absolute path would get us into a permission error when trying to write
    dest = Path(os.path.relpath(str(dest), "."))
    dest.mkdir(exist_ok=True, parents=True)
    for file in source.rglob("*.txtp"):
        file_name = file.name
        out_path = dest.joinpath(file_name.replace(".txtp", ".wav"))
        if not out_path.exists():
            subprocess.call(["vgmstream-cli", file, "-o", out_path], stdout=open(os.devnull, 'wb'))


audio_convert(Path(argv[1]), Path(argv[2]))
