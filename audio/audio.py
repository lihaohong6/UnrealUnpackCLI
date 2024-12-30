﻿import concurrent.futures
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


def txtp_to_wav(source: Path, dest: Path):
    
    dest.mkdir(exist_ok=True, parents=True)
    for file in source.rglob("*.txtp"):
        file_name = file.name
        out_path = dest.joinpath(file_name.replace(".txtp", ".wav"))
        def func(txtp_file, output_path):
            subprocess.run(["vgmstream-cli", txtp_file, "-o", output_path.absolute()],
                             stdout=open(os.devnull, 'wb'),
                             cwd=source.parent)
        func(file, out_path)


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


def make_txtp_files(audio_dir, txtp):
    wwiser_location = Path('wwiser/wwiser.pyz').absolute()
    config_file = Path(
        ''.join(random.choices(string.ascii_uppercase + string.digits, k=15)) + "wwconfig.txt").absolute()
    with open(config_file, "w") as f:
        f.write("-g -go ")
        f.write(f'"txtp" ')
        f.write(" ".join(f'"{str(p.relative_to(audio_dir))}"' for p in Path(audio_dir).glob('*.bnk')))
    subprocess.run(['python', wwiser_location, config_file],
                   cwd=audio_dir)
    config_file.unlink()


def generate_wav(audio_dir: Path, output_dir: Path):
    txtp = Path(audio_dir.absolute() / 'txtp')

    if txtp.exists():
        shutil.rmtree(txtp)

    make_txtp_files(audio_dir, txtp)

    txtp_to_wav(txtp, output_dir)


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
        Path(dir_name).mkdir()

    # Generate WAV files
    with concurrent.futures.ProcessPoolExecutor() as executor:
        for config in configs:
            executor.submit(generate_wav, 
                            config.audio_path, Path(config.output_dir))


if __name__ == "__main__":
    main()
