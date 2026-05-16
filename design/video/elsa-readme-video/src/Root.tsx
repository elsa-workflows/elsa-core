import { Composition } from "remotion";
import { ElsaReadmeVideo } from "./ElsaReadmeVideo";

export const RemotionRoot = () => {
  return (
    <Composition
      id="ElsaReadme"
      component={ElsaReadmeVideo}
      durationInFrames={330}
      fps={30}
      width={1280}
      height={720}
    />
  );
};
