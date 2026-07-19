using NUnit.Framework;
using TheDarkestNight;

namespace TheDarkestNight.Tests
{
    public partial class PureLogicTests
    {

        // ── AshenVisuals body-key transforms ──────────────────────────────────

        [Test]
        public void AshenVisuals_HairKey_SetsLightGreyD3D3()
        {
            ulong input  = 0xFFFFFFFFFFFFFFFFUL;
            ulong result = AshenVisuals.AshenHairKey(input);
            Assert.AreEqual(0x00D3D30000000000UL, result & 0x00FFFF0000000000UL,
                "Hair colour bytes must encode light grey #D3D3.");
            Assert.AreEqual(input  & ~0x00FFFF0000000000UL,
                            result & ~0x00FFFF0000000000UL,
                "Bits outside the hair colour byte range must be preserved.");
        }


        [Test]
        public void AshenVisuals_EyeKey_SetsColdBlueBytes()
        {
            ulong result = AshenVisuals.AshenEyeKey(0UL);
            Assert.AreEqual(0x00E0AA0000000000UL, result,
                "Eye transform must encode a cold-blue iris into the colour bytes.");
        }


        [Test]
        public void AshenVisuals_EyeKey_PreservesNonColourBits()
        {
            ulong input  = 0xAB00000000C0FFEEUL;
            ulong result = AshenVisuals.AshenEyeKey(input);
            Assert.AreEqual(input  & ~0x00FFFF0000000000UL,
                            result & ~0x00FFFF0000000000UL);
        }


        [Test]
        public void AshenVisuals_SkinKey_SetsLightGreyD3D3D3()
        {
            ulong input  = 0xFFFFFFFFFFFFFFFFUL;
            ulong result = AshenVisuals.AshenSkinKey(input);
            Assert.AreEqual(0x000000D3D3D30000UL, result & 0x000000FFFFFF0000UL,
                "Skin colour bytes must encode light grey #D3D3D3.");
            Assert.AreEqual(input & ~0x000000FFFFFF0000UL, result & ~0x000000FFFFFF0000UL,
                "All other bits must be preserved.");
        }


        [Test]
        public void AshenVisuals_Transforms_AreIdempotent()
        {
            ulong seed = 0x123456789ABCDEF0UL;
            Assert.AreEqual(AshenVisuals.AshenHairKey(seed),
                            AshenVisuals.AshenHairKey(AshenVisuals.AshenHairKey(seed)));
            Assert.AreEqual(AshenVisuals.AshenEyeKey(seed),
                            AshenVisuals.AshenEyeKey(AshenVisuals.AshenEyeKey(seed)));
            Assert.AreEqual(AshenVisuals.AshenSkinKey(seed),
                            AshenVisuals.AshenSkinKey(AshenVisuals.AshenSkinKey(seed)));
        }
    }
}
