import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Fragment } from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import Link from 'Components/Link/Link';
import { getYouTubeAlbumUrl, getYouTubeArtistUrl, getYouTubeVideoUrl } from 'Helpers/youTubeLinks';
import formatTimeSpan from 'Utilities/Date/formatTimeSpan';
import translate from 'Utilities/String/translate';
import styles from './FileDetails.css';

function renderIdField(titleKey, id, getUrl) {
  const url = getUrl(id);
  const item = (
    <DescriptionListItem
      title={translate(titleKey)}
      data={id}
    />
  );

  if (url) {
    return (
      <Link to={url}>
        {item}
      </Link>
    );
  }

  return item;
}

function renderRejections(rejections) {
  return (
    <span>
      <DescriptionListItemTitle>
        Rejections
      </DescriptionListItemTitle>
      {
        _.map(rejections, (item, key) => {
          return (
            <DescriptionListItemDescription key={key}>
              {item.reason}
            </DescriptionListItemDescription>
          );
        })
      }
    </span>
  );
}

function FileDetails(props) {

  const {
    filename,
    audioTags,
    rejections
  } = props;

  return (
    <Fragment>
      <div className={styles.audioTags}>
        <DescriptionList>
          {
            filename &&
              <DescriptionListItem
                title={translate('Filename')}
                data={filename}
                descriptionClassName={styles.filename}
              />
          }
          {
            audioTags.title !== undefined &&
              <DescriptionListItem
                title={translate('TrackTitle')}
                data={audioTags.title}
              />
          }
          {
            audioTags.trackNumbers[0] > 0 &&
              <DescriptionListItem
                title={translate('TrackNumber')}
                data={audioTags.trackNumbers[0]}
              />
          }
          {
            audioTags.discNumber > 0 &&
              <DescriptionListItem
                title={translate('DiscNumber')}
                data={audioTags.discNumber}
              />
          }
          {
            audioTags.discCount > 0 &&
              <DescriptionListItem
                title={translate('DiscCount')}
                data={audioTags.discCount}
              />
          }
          {
            audioTags.albumTitle !== undefined &&
              <DescriptionListItem
                title={translate('Album')}
                data={audioTags.albumTitle}
              />
          }
          {
            audioTags.artistTitle !== undefined &&
              <DescriptionListItem
                title={translate('Artist')}
                data={audioTags.artistTitle}
              />
          }
          {
            audioTags.country !== undefined &&
              <DescriptionListItem
                title={translate('Country')}
                data={audioTags.country.name}
              />
          }
          {
            audioTags.year > 0 &&
              <DescriptionListItem
                title={translate('Year')}
                data={audioTags.year}
              />
          }
          {
            audioTags.label !== undefined &&
              <DescriptionListItem
                title={translate('Label')}
                data={audioTags.label}
              />
          }
          {
            audioTags.catalogNumber !== undefined &&
              <DescriptionListItem
                title={translate('CatalogNumber')}
                data={audioTags.catalogNumber}
              />
          }
          {
            audioTags.disambiguation !== undefined &&
              <DescriptionListItem
                title={translate('Disambiguation')}
                data={audioTags.disambiguation}
              />
          }
          {
            audioTags.duration !== undefined &&
              <DescriptionListItem
                title={translate('Duration')}
                data={formatTimeSpan(audioTags.duration)}
              />
          }
          {
            audioTags.artistMBId !== undefined &&
              renderIdField('MusicBrainzArtistID', audioTags.artistMBId, getYouTubeArtistUrl)
          }
          {
            audioTags.albumMBId !== undefined &&
              renderIdField('MusicBrainzAlbumID', audioTags.albumMBId, getYouTubeAlbumUrl)
          }
          {
            audioTags.releaseMBId !== undefined &&
              renderIdField('MusicBrainzReleaseID', audioTags.releaseMBId, getYouTubeAlbumUrl)
          }
          {
            audioTags.recordingMBId !== undefined &&
              renderIdField('MusicBrainzRecordingID', audioTags.recordingMBId, getYouTubeVideoUrl)
          }
          {
            audioTags.trackMBId !== undefined &&
              renderIdField('MusicBrainzTrackID', audioTags.trackMBId, getYouTubeVideoUrl)
          }
          {
            !!rejections && rejections.length > 0 &&
              renderRejections(rejections)
          }
        </DescriptionList>
      </div>
    </Fragment>
  );
}

FileDetails.propTypes = {
  filename: PropTypes.string,
  audioTags: PropTypes.object.isRequired,
  rejections: PropTypes.arrayOf(PropTypes.object)
};

export default FileDetails;
