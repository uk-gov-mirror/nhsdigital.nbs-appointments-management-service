import React, { ReactNode, Children } from 'react';

type Props = {
  children: ReactNode;
  vertical?: boolean;
};

const ButtonGroup = ({ children, vertical = false }: Props) => {
  return (
    <ol
      className={`nhsuk-list nhsuk-u-margin-0 nhsuk-button-group-flat button-group ${
        vertical
          ? 'flex-col button-group-vertical'
          : 'flex-row button-group-horizontal'
      }`}
    >
      {Children.toArray(children).map((child, index) => (
        <li key={index} className="nhsuk-u-margin-0">
          {child}
        </li>
      ))}
    </ol>
  );
};

export default ButtonGroup;
