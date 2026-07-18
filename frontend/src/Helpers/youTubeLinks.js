export function getYouTubeArtistUrl(foreignArtistId) {
  if (!foreignArtistId) {
    return null;
  }

  const id = String(foreignArtistId);

  if (id.startsWith('yt:channel:')) {
    return `https://www.youtube.com/channel/${id.slice('yt:channel:'.length)}`;
  }

  if (id.startsWith('yt:uploads:')) {
    return `https://www.youtube.com/channel/${id.slice('yt:uploads:'.length)}/videos`;
  }

  if (id.startsWith('@') || id.startsWith('UC')) {
    return id.startsWith('@')
      ? `https://www.youtube.com/${id}`
      : `https://www.youtube.com/channel/${id}`;
  }

  return null;
}

export function getYouTubeAlbumUrl(foreignAlbumId) {
  if (!foreignAlbumId) {
    return null;
  }

  const id = String(foreignAlbumId);

  if (id.startsWith('yt:playlist:')) {
    return `https://www.youtube.com/playlist?list=${id.slice('yt:playlist:'.length)}`;
  }

  if (id.startsWith('yt:uploads:')) {
    return `https://www.youtube.com/channel/${id.slice('yt:uploads:'.length)}/videos`;
  }

  if (id.startsWith('yt:release:')) {
    // Release ids wrap playlist/uploads ids; strip prefix and reuse.
    return getYouTubeAlbumUrl(id.slice('yt:release:'.length));
  }

  if (id.startsWith('yt:video:')) {
    return `https://www.youtube.com/watch?v=${id.slice('yt:video:'.length)}`;
  }

  if (id.startsWith('PL') || id.startsWith('FL') || id.startsWith('UU')) {
    return `https://www.youtube.com/playlist?list=${id}`;
  }

  return null;
}
