# codesign --deep --force --verify --verbose --timestamp --options runtime --entitlements "HavocInHavana.entitlements" --sign "Developer ID Application: Brian Fitzgerald (V7U7596DAW)" "havocinhavana.app"

# # jmxp-lxlq-lzsu-guvi

# ditto -c -k --sequesterRsrc --keepParent "havocinhavana.app" "havocinhavana.zip"

# # not needed - just gets the short name
# # xcrun iTMSTransporter -m provider -u cooldude242@gmail.com -p jmxp-lxlq-lzsu-guvi
# # short name: V7U7596DAW

# xcrun altool --notarize-app --username cooldude242@gmail.com --password jmxp-lxlq-lzsu-guvi --asc-provider V7U7596DAW --primary-bundle-id com.brianfitzgerald.havocinhavana --file havocinhavana.zip

# # wait 1 hour

# # replace notarization-info with RequestUUId from above
# xcrun altool --notarization-info ef1eb4b4-55e5-4c0e-9499-742b5012375b --username cooldude242@gmail.com --password jmxp-lxlq-lzsu-guvi --asc-provider V7U7596DAW

# xcrun stapler staple "havocinhavana.app"

# spctl -a -v havocinhavana.app

# notarize-app --cert "Developer ID Application: Brian Fitzgerald (V7U7596DAW)"  --username cooldude242@gmail.com --pwd jmxp-lxlq-lzsu-guvi --provider V7U7596DAW

cd ../ttt_build

# ditto -c -k --sequesterRsrc --keepParent "havocinhavana.app" "havocinhavana.zip"

notarize-app --cert "Developer ID Application: Brian Fitzgerald (V7U7596DAW)"  --username cooldude242@gmail.com --pwd jmxp-lxlq-lzsu-guvi --provider V7U7596DAW

xcrun altool --notarization-info ef1eb4b4-55e5-4c0e-9499-742b5012375b --username cooldude242@gmail.com --password jmxp-lxlq-lzsu-guvi --asc-provider V7U7596DAW

xcrun stapler staple "havocinhavana.app"

aws s3 cp ~/projects/ttt_build/notarize/ s3://ttt-builds/ --recursive --acl public-read --profile personal