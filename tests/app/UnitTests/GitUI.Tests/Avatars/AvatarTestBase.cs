﻿using System.Drawing.Imaging;
using GitUI.Avatars;
using NSubstitute;

namespace GitUITests.Avatars
{
    public abstract class AvatarTestBase
    {
        protected const int _size = 16;

        protected const string _email1 = "a@a.a";
        protected const string _email2 = "b@b.b";
        protected const string _email3 = "c@c.c";
        protected const string _email4 = "d@d.d";
        protected const string _emailMissing = "missing@avatar.com";

        protected const string _name1 = "John Lennon";
        protected const string _name2 = "Paul McCartney";
        protected const string _name3 = "George Harrison";
        protected const string _name4 = "Ringo Starr";
        protected const string _nameMissing = "Fifth Beatle";

        protected Image _img1;
        protected Image _img2;
        protected Image _img3;
        protected Image _img4;
        protected Image _imgGenerated;

        protected IAvatarProvider _inner;
        protected IAvatarProvider _cache;
        protected IAvatarCacheCleaner _cacheCleaner => _cache as IAvatarCacheCleaner;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _img1 = new Bitmap(_size, _size);
            _img2 = new Bitmap(_size, _size);
            _img3 = new Bitmap(_size, _size);
            _img4 = new Bitmap(_size, _size);
            _imgGenerated = new Bitmap(_size, _size);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _img1.Dispose();
            _img2.Dispose();
            _img3.Dispose();
            _img4.Dispose();
            _imgGenerated.Dispose();
        }

        [SetUp]
        public virtual void SetUp()
        {
            _inner = Substitute.For<IAvatarProvider>();

            _inner.PerformsIo.Returns(true);
            _inner.GetAvatarAsync(_email1, _name1, _size).Returns(Task.FromResult(_img1));
            _inner.GetAvatarAsync(_email2, _name2, _size).Returns(Task.FromResult(_img2));
            _inner.GetAvatarAsync(_email3, _name3, _size).Returns(Task.FromResult(_img3));
            _inner.GetAvatarAsync(_email4, _name4, _size).Returns(Task.FromResult(_img4));
            _inner.GetAvatarAsync(_emailMissing, _nameMissing, _size).Returns(Task.FromResult((Image)null));
        }

        protected async Task MissAsync(string email, string name,  Image expected = null)
        {
            _inner.ClearReceivedCalls();

            Image actual = await _cache.GetAvatarAsync(email, name, _size);

            _ = _inner.Received(1).GetAvatarAsync(email, name, _size);

            if (expected is not null)
            {
                ClassicAssert.AreSame(expected, actual);
            }
        }

        protected async Task HitAsync(string email, string name, Image expected = null)
        {
            _inner.ClearReceivedCalls();

            Image actual = await _cache.GetAvatarAsync(email, name, _size);

            _ = _inner.Received(0).GetAvatarAsync(email, name, _size);

            if (expected is not null)
            {
                ClassicAssert.AreSame(expected, actual);
            }
        }

        protected Stream GetPngStream()
        {
            MemoryStream stream = new();
            _img1.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            return stream;
        }
    }
}
