﻿using GitUIPluginInterfaces;

namespace GitCommandsTests.Git
{
    [TestFixture]
    public sealed class GitStashTests
    {
        [TestCase("stash@{0}: Very descriptive message", 0, "stash@{0}", "Very descriptive message")]
        [TestCase("stash@{1}: On 4263_View_Stash_AOORE: Testing things", 1, "stash@{1}", "On 4263_View_Stash_AOORE: Testing things")]
        [TestCase("stash@{2}: WIP on 4263_View_Stash_AOORE: a8348732f Merge pull request #4259 from RussKie/fix_4258_NRE_in_FormPull", 2, "stash@{2}", "WIP on 4263_View_Stash_AOORE: a8348732f Merge pull request #4259 from RussKie/fix_4258_NRE_in_FormPull")]
        [TestCase("stash@{3}: WIP on master: Test", 3, "stash@{3}", "WIP on master: Test")]
        public void Can_parse_stash_names(string rawStash, int index, string name, string message)
        {
            ClassicAssert.IsTrue(GitStash.TryParse(rawStash, out GitStash? stash));

            ClassicAssert.NotNull(stash);
            ClassicAssert.AreEqual(index, stash.Index);
            ClassicAssert.AreEqual(message, stash.Message);
            ClassicAssert.AreEqual(name, stash.Name);
        }

        [TestCase("stash@{0}:Very descriptive message")]
        [TestCase("stash@{-1}: Very descriptive message")]
        [TestCase("stash{0}: Very descriptive message")]
        [TestCase(" stash@{0}: Very descriptive message")]
        [TestCase("stash@{0}: ")]
        [TestCase("")]
        [TestCase("  ")]
        public void Identifies_invalid_stash_strings(string rawStash)
        {
            ClassicAssert.IsFalse(GitStash.TryParse(rawStash, out GitStash? stash));
            ClassicAssert.Null(stash);
        }
    }
}
