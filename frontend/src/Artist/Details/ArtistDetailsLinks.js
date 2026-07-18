import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import { getYouTubeArtistUrl } from 'Helpers/youTubeLinks';
import { kinds, sizes } from 'Helpers/Props';
import styles from './ArtistDetailsLinks.css';

function ArtistDetailsLinks(props) {
  const {
    foreignArtistId,
    links
  } = props;

  const youtubeUrl = getYouTubeArtistUrl(foreignArtistId);
  const hasYoutubeLink = (links || []).some((link) =>
    String(link.name || '').toLowerCase() === 'youtube' ||
    String(link.url || '').includes('youtube.com')
  );

  return (
    <div className={styles.links}>

      {
        youtubeUrl && !hasYoutubeLink ?
          <Link
            className={styles.link}
            to={youtubeUrl}
          >
            <Label
              className={styles.linkLabel}
              kind={kinds.INFO}
              size={sizes.LARGE}
            >
              YouTube
            </Label>
          </Link> :
          null
      }

      {(links || []).map((link, index) => {
        return (
          <span key={index}>
            <Link
              className={styles.link}
              to={link.url}
            >
              <Label
                className={styles.linkLabel}
                kind={kinds.INFO}
                size={sizes.LARGE}
              >
                {link.name}
              </Label>
            </Link>
            {(index > 0 && index % 5 === 0) &&
              <br />
            }

          </span>
        );
      })}

    </div>

  );
}

ArtistDetailsLinks.propTypes = {
  foreignArtistId: PropTypes.string.isRequired,
  links: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default ArtistDetailsLinks;
