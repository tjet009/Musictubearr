import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import { icons } from 'Helpers/Props';
import styles from './HeartRating.css';

function HeartRating({ rating = 0, iconSize = 14 }) {
  const safeRating = typeof rating === 'number' && !Number.isNaN(rating) ? rating : 0;

  return (
    <span className={styles.rating}>
      <Icon
        className={styles.heart}
        name={icons.HEART}
        size={iconSize}
      />

      {safeRating * 10}%
    </span>
  );
}

HeartRating.propTypes = {
  rating: PropTypes.number,
  iconSize: PropTypes.number
};

export default HeartRating;
