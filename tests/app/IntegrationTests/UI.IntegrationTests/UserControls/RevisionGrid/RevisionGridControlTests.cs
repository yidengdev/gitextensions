﻿using System.Diagnostics;
using CommonTestUtils;
using FluentAssertions;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI;
using GitUI.CommandsDialogs;
using GitUIPluginInterfaces;

namespace GitExtensions.UITests.UserControls.RevisionGrid
{
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class RevisionGridControlTests
    {
        // Created once for the fixture
        private ReferenceRepository _referenceRepository;
        private string _initialCommit;
        private string _headCommit;
        private string _branch1Commit;

        // Created once for each test
        private GitUICommands _commands;

        [OneTimeSetUp]
        public void SetUpFixture()
        {
            // There is no need to restore the original AppSettings because AppSettings is routed to a temp folder.

            // We don't want avatars during tests, otherwise we will be attempting to download them from gravatar.
            AppSettings.ShowAuthorAvatarColumn = false;
        }

        [SetUp]
        public void SetUp()
        {
            _referenceRepository = new ReferenceRepository();
            _initialCommit = _referenceRepository.CommitHash;

            _referenceRepository.CreateCommit("Commit1", "Commit1");
            _branch1Commit = _referenceRepository.CommitHash;
            _referenceRepository.CreateBranch("Branch1", _branch1Commit);
            _referenceRepository.CreateCommit("Commit2", "Commit2");
            _referenceRepository.CreateBranch("Branch2", _referenceRepository.CommitHash);

            _referenceRepository.CreateCommit("head commit");
            _headCommit = _referenceRepository.CommitHash;

            _commands = new GitUICommands(GlobalServiceContainer.CreateDefaultMockServiceContainer(), _referenceRepository.Module);

            AppSettings.RevisionGraphShowArtificialCommits = true;
        }

        [TearDown]
        public void TearDown()
        {
            _referenceRepository.Dispose();
        }

        [Test]
        public void Assert_default_filter_related_settings()
        {
            AppSettings.ShowReflogReferences.Value = false;
            AppSettings.BranchFilterEnabled.Value = false;
            AppSettings.ShowCurrentBranchOnly.Value = false;
            AppSettings.ShowGitNotes = true;

            RunSetAndApplyBranchFilterTest(
                initialFilter: "",
                grepMessage: "",
                revisionGridControl =>
                {
                    ClassicAssert.False(AppSettings.BranchFilterEnabled);
                    ClassicAssert.False(AppSettings.ShowCurrentBranchOnly);

                    ClassicAssert.True(revisionGridControl.CurrentFilter.IsShowAllBranchesChecked);
                    ClassicAssert.False(revisionGridControl.CurrentFilter.IsShowCurrentBranchOnlyChecked);
                    ClassicAssert.False(revisionGridControl.CurrentFilter.IsShowFilteredBranchesChecked);
                });

            RunSetAndApplyBranchFilterTest(
                initialFilter: "Branch1",
                grepMessage: "",
                revisionGridControl =>
                {
                    ClassicAssert.True(AppSettings.BranchFilterEnabled);
                    ClassicAssert.False(AppSettings.ShowCurrentBranchOnly);

                    ClassicAssert.False(revisionGridControl.CurrentFilter.IsShowAllBranchesChecked);
                    ClassicAssert.False(revisionGridControl.CurrentFilter.IsShowCurrentBranchOnlyChecked);
                    ClassicAssert.True(revisionGridControl.CurrentFilter.IsShowFilteredBranchesChecked);
                });

            RunSetAndApplyBranchFilterTest(
                initialFilter: "",
                grepMessage: "Commit1",
                revisionGridControl =>
                {
                    ClassicAssert.False(AppSettings.BranchFilterEnabled);
                    ClassicAssert.False(AppSettings.ShowCurrentBranchOnly);

                    ClassicAssert.True(revisionGridControl.CurrentFilter.IsShowAllBranchesChecked);
                    ClassicAssert.False(revisionGridControl.CurrentFilter.IsShowCurrentBranchOnlyChecked);
                    ClassicAssert.False(revisionGridControl.CurrentFilter.IsShowFilteredBranchesChecked);
                });
        }

        [Test]
        public void View_reflects_applied_branch_filter()
        {
            AppSettings.ShowReflogReferences.Value = false;
            AppSettings.BranchFilterEnabled.Value = false;
            AppSettings.ShowCurrentBranchOnly.Value = false;
            AppSettings.ShowGitNotes = true;

            RunSetAndApplyBranchFilterTest(
                initialFilter: "",
                grepMessage: "",
                revisionGridControl =>
                {
                    WaitForRevisionsToBeLoaded(revisionGridControl);

                    RevisionGridControl.TestAccessor ta = revisionGridControl.GetTestAccessor();
                    ClassicAssert.False(revisionGridControl.CurrentFilter.IsShowFilteredBranchesChecked);
#if DEBUG
                    // https://github.com/gitextensions/gitextensions/issues/10170
                    // This test occasionaly fails with 3 visible revisions
                    ta.VisibleRevisionCount.Should().Be(4);
#endif

                    // Verify the view hasn't changed until we refresh
                    revisionGridControl.LatestSelectedRevision.ObjectId.ToString().Should().Be(_headCommit);

                    // set filter
                    revisionGridControl.SetAndApplyBranchFilter("Branch1");
                    ClassicAssert.True(revisionGridControl.CurrentFilter.IsShowFilteredBranchesChecked);

#if DEBUG
                    // https://github.com/gitextensions/gitextensions/issues/10170
                    // This step occasionaly fails with 'Loading Revisions' didn't finish in 25 iterations
                    WaitForRevisionsToBeLoaded(revisionGridControl);

                    // Confirm the filter has been applied
                    // This test occasionaly fails with 4 visible revisions
                    ta.VisibleRevisionCount.Should().Be(2);
#endif
                });
        }

        [Test]
        public void View_reflects_reset_branch_filter()
        {
            AppSettings.ShowReflogReferences.Value = false;
            AppSettings.BranchFilterEnabled.Value = false;
            AppSettings.ShowCurrentBranchOnly.Value = false;
            AppSettings.ShowGitNotes = true;

            RunSetAndApplyBranchFilterTest(
                initialFilter: "Branch1",
                grepMessage: "",
                revisionGridControl =>
                {
                    WaitForRevisionsToBeLoaded(revisionGridControl);

                    RevisionGridControl.TestAccessor ta = revisionGridControl.GetTestAccessor();
                    ClassicAssert.True(revisionGridControl.CurrentFilter.IsShowFilteredBranchesChecked);
                    ta.VisibleRevisionCount.Should().Be(2);

                    // Verify the view hasn't changed until we refresh
                    revisionGridControl.LatestSelectedRevision.ObjectId.ToString().Should().Be(_branch1Commit);

                    // reset filter
                    revisionGridControl.SetAndApplyBranchFilter(string.Empty);
                    ClassicAssert.False(revisionGridControl.CurrentFilter.IsShowFilteredBranchesChecked);

#if DEBUG
                    // https://github.com/gitextensions/gitextensions/issues/10170
                    // This step occasionaly fails with 'Loading Revisions' didn't finish in 25 iterations
                    WaitForRevisionsToBeLoaded(revisionGridControl);

                    // Confirm the filter has been reset, all commits are shown
                    // This test occasionaly fails with 3 visible revisions
                    ta.VisibleRevisionCount.Should().Be(4);
#endif
                });
        }

        [Test]
        public void ToggleBetweenArtificialAndHeadCommits_no_empty([Values(false, true)] bool showGitStatusForArtificialCommits)
        {
            File.WriteAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "new.txt"), "new");
            File.WriteAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "stage.txt"), "staged");
            _referenceRepository.Module.StageFile("stage.txt");

            RunToggleBetweenArtificialAndHeadCommitsTest(
                showGitStatusForArtificialCommits,
                revisionGridControl =>
                {
                    // If showGitStatusForArtificialCommits is false, we do not update ChangeCount and HasChanges returns false.
                    // Then ToggleBetweenArtificialAndHeadCommits does not check HasChanges and toggles through all three commits.
                    while (revisionGridControl.GetChangeCount(ObjectId.WorkTreeId).HasChanges != showGitStatusForArtificialCommits
                        || revisionGridControl.GetChangeCount(ObjectId.IndexId).HasChanges != showGitStatusForArtificialCommits)
                    {
                        DoEvents();
                    }

                    revisionGridControl.GoToRef(_initialCommit, showNoRevisionMsg: false);
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(_initialCommit);

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.WorkTreeGuid);

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.IndexGuid);

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(_headCommit);

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.WorkTreeGuid);
                });
        }

        [Test]
        public void ToggleBetweenArtificialAndHeadCommits_no_workdir_change([Values(false, true)] bool showGitStatusForArtificialCommits)
        {
            File.WriteAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "stage.txt"), "staged");
            _referenceRepository.Module.StageFile("stage.txt");

            RunToggleBetweenArtificialAndHeadCommitsTest(
                showGitStatusForArtificialCommits,
                revisionGridControl =>
                {
                    while (revisionGridControl.GetChangeCount(ObjectId.WorkTreeId).HasChanges != false
                        || revisionGridControl.GetChangeCount(ObjectId.IndexId).HasChanges != showGitStatusForArtificialCommits)
                    {
                        DoEvents();
                    }

                    revisionGridControl.GoToRef(_initialCommit, showNoRevisionMsg: false);
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(_initialCommit);

                    if (!showGitStatusForArtificialCommits)
                    {
                        revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                        revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.WorkTreeGuid);
                    }

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.IndexGuid);

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(_headCommit);

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid
                        .Should().Be(showGitStatusForArtificialCommits ? GitRevision.IndexGuid : GitRevision.WorkTreeGuid);
                });
        }

        [Test]
        public void ToggleBetweenArtificialAndHeadCommits_no_index_change([Values(false, true)] bool showGitStatusForArtificialCommits)
        {
            File.WriteAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "new.txt"), "new");

            RunToggleBetweenArtificialAndHeadCommitsTest(
                showGitStatusForArtificialCommits,
                revisionGridControl =>
                {
                    while (revisionGridControl.GetChangeCount(ObjectId.WorkTreeId).HasChanges != showGitStatusForArtificialCommits
                        || revisionGridControl.GetChangeCount(ObjectId.IndexId).HasChanges != false)
                    {
                        DoEvents();
                    }

                    revisionGridControl.GoToRef(_initialCommit, showNoRevisionMsg: false);
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(_initialCommit);

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.WorkTreeGuid);

                    if (!showGitStatusForArtificialCommits)
                    {
                        revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                        revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.IndexGuid);
                    }

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(_headCommit);

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.WorkTreeGuid);
                });
        }

        [Test]
        public void ToggleBetweenArtificialAndHeadCommits_no_change([Values(false, true)] bool showGitStatusForArtificialCommits)
        {
            RunToggleBetweenArtificialAndHeadCommitsTest(
                showGitStatusForArtificialCommits,
                revisionGridControl =>
                {
                    while (revisionGridControl.GetChangeCount(ObjectId.WorkTreeId).HasChanges != false
                        || revisionGridControl.GetChangeCount(ObjectId.IndexId).HasChanges != false)
                    {
                        DoEvents();
                    }

                    revisionGridControl.GoToRef(_initialCommit, showNoRevisionMsg: false);
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(_initialCommit);

                    if (!showGitStatusForArtificialCommits)
                    {
                        revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                        revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.WorkTreeGuid);

                        revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                        revisionGridControl.LatestSelectedRevision.Guid.Should().Be(GitRevision.IndexGuid);
                    }

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid.Should().Be(_headCommit);

                    revisionGridControl.ToggleBetweenArtificialAndHeadCommits();
                    revisionGridControl.LatestSelectedRevision.Guid
                        .Should().Be(showGitStatusForArtificialCommits ? _headCommit : GitRevision.WorkTreeGuid);
                });
        }

        private void RunSetAndApplyBranchFilterTest(string initialFilter, string grepMessage, Action<RevisionGridControl> runTest)
        {
            // Disable artificial commits as they appear to destabilise these tests
            AppSettings.RevisionGraphShowArtificialCommits = false;

            UITest.RunForm<FormBrowse>(
                showForm: () => _commands.StartBrowseDialog(owner: null).Should().BeTrue(),
                runTestAsync: async formBrowse =>
                {
                    DoEvents();

                    // wait for the revisions to be loaded
                    await AsyncTestHelper.JoinPendingOperationsAsync(AsyncTestHelper.UnexpectedTimeout);

                    formBrowse.RevisionGridControl.SetSelectedRevision(ObjectId.Parse(_headCommit)).Should().BeTrue();

                    formBrowse.RevisionGridControl.CurrentFilter.ByMessage = !string.IsNullOrWhiteSpace(grepMessage);
                    formBrowse.RevisionGridControl.CurrentFilter.Message = grepMessage;
                    formBrowse.RevisionGridControl.SetAndApplyBranchFilter(initialFilter);

                    // wait for the revisions to be loaded
                    await AsyncTestHelper.JoinPendingOperationsAsync(AsyncTestHelper.UnexpectedTimeout);

                    try
                    {
                        runTest(formBrowse.RevisionGridControl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{runTest.Method.Name} failed: {ex.Demystify()}");
                        Console.WriteLine(_referenceRepository.Module.GitExecutable.GetOutput("status"));
                        throw;
                    }
                });
        }

        private void RunToggleBetweenArtificialAndHeadCommitsTest(bool showGitStatusForArtificialCommits, Action<RevisionGridControl> runTest)
        {
            AppSettings.ShowGitStatusForArtificialCommits = showGitStatusForArtificialCommits;

            UITest.RunForm<FormBrowse>(
                showForm: () => _commands.StartBrowseDialog(owner: null).Should().BeTrue(),
                runTestAsync: async formBrowse =>
                {
                    DoEvents();

                    // wait for the revisions to be loaded
                    await AsyncTestHelper.JoinPendingOperationsAsync(AsyncTestHelper.UnexpectedTimeout);

                    formBrowse.RevisionGridControl.LatestSelectedRevision.Guid.Should().Be(_headCommit);

                    FormBrowse.TestAccessor ta = formBrowse.GetTestAccessor();
                    ta.CommitInfoTabControl.SelectedTab = ta.TreeTabPage;

                    // let the focus events be handled
                    DoEvents();

                    try
                    {
                        runTest(formBrowse.RevisionGridControl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{runTest.Method.Name} failed: {ex.Demystify()}");
                        Console.WriteLine(_referenceRepository.Module.GitExecutable.GetOutput("status"));
                        throw;
                    }

                    // let the focus events be handled
                    DoEvents();

                    ClassicAssert.IsTrue(ta.CommitInfoTabControl.SelectedTab == ta.DiffTabPage, "Diff tab should be active");
                });
        }

        private static void DoEvents()
        {
            for (int i = 0; i < 5; ++i)
            {
                Thread.Sleep(50);
                Application.DoEvents();
            }
        }

        private static void WaitForRevisionsToBeLoaded(RevisionGridControl revisionGridControl)
        {
            UITest.ProcessUntil("Loading Revisions", () => revisionGridControl.GetTestAccessor().IsDataLoadComplete);
        }
    }
}
