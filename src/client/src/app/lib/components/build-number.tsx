'use server';

const BuildNumber = () => {
  const buildNumberText = `Build number: ${process.env.BUILD_NUMBER}`;

  return (
    <span aria-hidden className="display-none">
      {buildNumberText}
    </span>
  );
};

export default BuildNumber;
