audio_root="../CNExport/WwiseAudio/Windows"
rm -rf banks
mkdir -m 744 -p banks
python wwiser/wwiser.pyz $audio_root/Chinese/*.bnk && mv banks.xml banks/cn_banks.xml
python wwiser/wwiser.pyz $audio_root/*.bnk && mv banks.xml banks/sfx_banks.xml
python wwiser/wwiser.pyz $audio_root/Japanese/*.bnk && mv banks.xml banks/ja_banks.xml

generate_wav() {
  (
  cd "$1" || echo "cd failed"
  rm -rf txtp
  python "$OLDPWD"/wwiser/wwiser.pyz -g ./*.bnk
  python "$OLDPWD"/audio.py ./txtp "$OLDPWD"/"$2"
  )
}

mkdir -m 744 Chinese Japanese sfx -p
generate_wav $audio_root/Chinese Chinese
generate_wav $audio_root/Japanese Japanese
generate_wav $audio_root sfx
