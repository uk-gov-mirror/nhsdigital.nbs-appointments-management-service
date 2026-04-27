'use client';

import { Button } from 'nhsuk-react-components';

const PrintPageButton = () => {
  const onClick = () => {
    window.print();
  };

  return (
    <Button className="no-print" small secondarySolid onClick={() => onClick()}>
      Print page
    </Button>
  );
};

export default PrintPageButton;
