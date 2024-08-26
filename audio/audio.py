import os
import shutil
import subprocess
import sys
from pathlib import Path
from sys import argv

config_file = Path("wwconfig.txt")


def audio_convert(source: Path, dest: Path):
    # this relpath step is necessary because cygwin has some odd admission control on /cygdrive/d
    # using the absolute path would get us into a permission error when trying to write
    dest = Path(os.path.relpath(str(dest), "."))
    dest.mkdir(exist_ok=True, parents=True)
    for file in source.rglob("*.txtp"):
        file_name = file.name
        out_path = dest.joinpath(file_name.replace(".txtp", ".wav"))
        if not out_path.exists():
            subprocess.call(["vgmstream-cli", file, "-o", out_path],
                            stdout=open(os.devnull, 'wb'),
                            cwd=str(source.parent))


def main():
    def generate_wav(audio_dir, output_dir):
        current_dir = Path.cwd()
        os.chdir(audio_dir)
        txtp = Path('txtp')
    
        if txtp.exists():
            shutil.rmtree(txtp)

        with open(config_file, "w") as f:
            f.write("-g -go ")
            f.write(f'"{str(txtp)}" ')
            f.write(" ".join(f'"{str(p)}"' for p in Path(".").glob('*.bnk')))
        subprocess.run(['python', current_dir / 'wwiser/wwiser.pyz', config_file])
        config_file.unlink()
        audio_convert(txtp, current_dir / output_dir)
        os.chdir(current_dir)

    # Set paths
    if len(sys.argv) > 1:
        audio_root = Path(sys.argv[1])
    else:
        audio_root = Path('..') / 'CNExport' / 'WwiseAudio' / 'Windows'
    print("Audio root " + str(audio_root))

    banks_dir = Path('banks')
    if banks_dir.exists():
        shutil.rmtree(banks_dir)
    banks_dir.mkdir(exist_ok=True)

    # Process bnk files
    configs = [
        (audio_root / 'Chinese', 'cn_banks.xml'),
        (audio_root / 'Japanese', 'ja_banks.xml'),
        (audio_root, 'sfx_banks.xml')
    ]
    for audio_path, xml_file in configs:
        with open(config_file, "w") as f:
            f.write(" ".join(f'"{str(p)}"' for p in audio_path.glob('*.bnk')))
        subprocess.run(['python', 'wwiser/wwiser.pyz', config_file])
        Path('banks.xml').rename(banks_dir / xml_file)

    # Create output directories
    for dir_name in ['Chinese', 'Japanese', 'sfx']:
        Path(dir_name).mkdir(exist_ok=True)

    # Generate WAV files
    generate_wav(audio_root / 'Chinese', 'Chinese')
    generate_wav(audio_root / 'Japanese', 'Japanese')
    generate_wav(audio_root, 'sfx')
    
    config_file.unlink(missing_ok=True)


if __name__ == "__main__":
    main()
