import concurrent.futures
import os
import random
import shutil
import string
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


@dataclass
class AudioConfig:
    audio_path: Path
    bank_file: str
    output_dir: str


def get_configs(root: Path, root_gl: Path):
    return [
        AudioConfig(root_gl / 'Chinese', 'cn_banks.xml', 'Chinese'),
        AudioConfig(root_gl / 'Japanese', 'ja_bank.xml', 'Japanese'),
        AudioConfig(root_gl / 'English', 'en_banks.xml', 'English'),
        AudioConfig(root_gl, 'sfx_banks.xml', 'sfx')
    ]


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


def make_bank_file(banks_dir, config):
    out_string = " ".join(f'"{str(p)}"' for p in config.audio_path.glob('*.bnk'))
    if out_string == "":
        return
    out_file = Path(config.bank_file)
    out_string += f' --output={config.bank_file.replace(".xml", "")}'
    config_file = Path(f"wwconfig{config.bank_file}.txt")
    with open(config_file, "w") as f:
        f.write(out_string)
    subprocess.run(['python', 'wwiser/wwiser.pyz', config_file])
    config_file.unlink()
    
    out_file.rename(banks_dir / config.bank_file)


def generate_wav(audio_dir, output_dir):
    txtp = Path(audio_dir.absolute() / 'txtp')

    if txtp.exists():
        shutil.rmtree(txtp)
    
    wwiser_location = Path('wwiser/wwiser.pyz').absolute()
    config_file = Path(''.join(random.choices(string.ascii_uppercase + string.digits, k=15)) + "wwconfig.txt").absolute()
    with open(config_file, "w") as f:
        f.write("-g -go ")
        f.write(f'"{str(txtp)}" ')
        f.write(" ".join(f'"{str(p)}"' for p in Path(".").glob('*.bnk')))
    subprocess.run(['python', wwiser_location, config_file],
                    cwd=audio_dir)
    config_file.unlink()
    
    audio_convert(txtp, output_dir)


def main():

    # Set paths
    if len(sys.argv) > 1:
        audio_root = Path(sys.argv[1])
        audio_root_en = Path(sys.argv[2])
    else:
        audio_root = Path('..') / 'CNExport' / 'WwiseAudio' / 'Windows'
        audio_root_en = Path('..') / 'GLExport' / 'WwiseAudio' / 'Windows'
    print("Audio root " + str(audio_root))
    print("Audio root en " + str(audio_root_en))

    banks_dir = Path('banks')
    if banks_dir.exists():
        shutil.rmtree(banks_dir)
    banks_dir.mkdir(exist_ok=True)

    configs = get_configs(audio_root, audio_root_en)
    
    # Process bnk files
    with concurrent.futures.ProcessPoolExecutor() as executor:
        for config in configs:
            executor.submit(make_bank_file, 
                            banks_dir, config)

    # Reset output directories
    for config in configs:
        dir_name = config.output_dir
        shutil.rmtree(dir_name, ignore_errors=True)
        Path(dir_name).mkdir(exist_ok=True)

    # Generate WAV files
    for config in configs:
        generate_wav(config.audio_path, config.output_dir)


if __name__ == "__main__":
    main()
