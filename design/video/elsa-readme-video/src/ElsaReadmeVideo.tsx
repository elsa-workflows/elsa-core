import React from "react";
import {
  AbsoluteFill,
  Easing,
  Img,
  interpolate,
  spring,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";

const colors = {
  ink: "#102233",
  teal: "#17a6a6",
  coral: "#ff6b57",
  gold: "#f5b84b",
  sky: "#dff5ff",
  mist: "#f5fbff",
  white: "#ffffff",
  slate: "#5b6b79",
  line: "rgba(16, 34, 51, 0.14)",
};

const ease = Easing.bezier(0.16, 1, 0.3, 1);

const clamp = {
  extrapolateLeft: "clamp" as const,
  extrapolateRight: "clamp" as const,
};

const sceneOpacity = (frame: number, start: number, end: number) => {
  const fadeIn = interpolate(frame, [start, start + 18], [0, 1], clamp);
  const fadeOut = interpolate(frame, [end - 18, end], [1, 0], clamp);
  return Math.min(fadeIn, fadeOut);
};

const rise = (frame: number, start: number, amount = 36) =>
  interpolate(frame, [start, start + 24], [amount, 0], {
    ...clamp,
    easing: ease,
  });

const draw = (frame: number, start: number, end: number) =>
  interpolate(frame, [start, end], [0, 1], {
    ...clamp,
    easing: ease,
  });

type SceneProps = {
  start: number;
  end: number;
  children: (progress: number, opacity: number) => React.ReactNode;
};

const Scene = ({ start, end, children }: SceneProps) => {
  const frame = useCurrentFrame();
  const opacity = sceneOpacity(frame, start, end);
  const progress = draw(frame, start, end);
  return (
    <AbsoluteFill style={{ opacity, pointerEvents: "none" }}>
      {children(progress, opacity)}
    </AbsoluteFill>
  );
};

const Background = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const drift = Math.sin(frame / (fps * 1.5)) * 12;

  return (
    <AbsoluteFill
      style={{
        background:
          "radial-gradient(circle at 20% 16%, rgba(23,166,166,0.22), transparent 28%), radial-gradient(circle at 78% 18%, rgba(255,107,87,0.18), transparent 24%), linear-gradient(135deg, #f7fcff 0%, #e9f8fb 48%, #fff4eb 100%)",
        overflow: "hidden",
      }}
    >
      <div
        style={{
          position: "absolute",
          inset: 0,
          backgroundImage:
            "linear-gradient(rgba(16,34,51,0.055) 1px, transparent 1px), linear-gradient(90deg, rgba(16,34,51,0.055) 1px, transparent 1px)",
          backgroundSize: "48px 48px",
          transform: `translate(${drift}px, ${drift * 0.4}px)`,
        }}
      />
      <div
        style={{
          position: "absolute",
          width: 820,
          height: 820,
          borderRadius: "50%",
          right: -260,
          bottom: -360,
          background:
            "conic-gradient(from 90deg, rgba(23,166,166,0.2), rgba(245,184,75,0.18), rgba(255,107,87,0.18), rgba(23,166,166,0.2))",
          filter: "blur(1px)",
          transform: `rotate(${frame * 0.18}deg)`,
        }}
      />
    </AbsoluteFill>
  );
};

const LogoMark = ({ scale = 1 }: { scale?: number }) => (
  <div
    style={{
      width: 118 * scale,
      height: 118 * scale,
      borderRadius: 30 * scale,
      background: colors.white,
      boxShadow: "0 18px 42px rgba(16, 34, 51, 0.16)",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      overflow: "hidden",
    }}
  >
    <Img
      src={staticFile("elsa-logo-art.png")}
      style={{ width: "82%", height: "82%", objectFit: "contain" }}
    />
  </div>
);

const Pill = ({ children, color }: { children: React.ReactNode; color: string }) => (
  <div
    style={{
      border: `2px solid ${color}`,
      color,
      borderRadius: 999,
      padding: "11px 18px",
      fontSize: 24,
      fontWeight: 800,
      background: "rgba(255,255,255,0.8)",
      boxShadow: "0 12px 24px rgba(16,34,51,0.08)",
    }}
  >
    {children}
  </div>
);

const Connector = ({
  x1,
  y1,
  x2,
  y2,
  progress,
}: {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  progress: number;
}) => {
  const dx = (x2 - x1) * progress;
  const dy = (y2 - y1) * progress;

  return (
    <svg style={{ position: "absolute", inset: 0 }} viewBox="0 0 1280 720">
      <line
        x1={x1}
        y1={y1}
        x2={x1 + dx}
        y2={y1 + dy}
        stroke={colors.teal}
        strokeWidth="5"
        strokeLinecap="round"
        strokeDasharray="14 14"
      />
    </svg>
  );
};

const WorkflowNode = ({
  label,
  x,
  y,
  color,
  delay,
}: {
  label: string;
  x: number;
  y: number;
  color: string;
  delay: number;
}) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const pop = spring({ frame: frame - delay, fps, config: { damping: 18, stiffness: 130 } });

  return (
    <div
      style={{
        position: "absolute",
        left: x,
        top: y,
        width: 174,
        height: 86,
        borderRadius: 24,
        background: colors.white,
        border: `4px solid ${color}`,
        boxShadow: "0 18px 38px rgba(16,34,51,0.14)",
        color: colors.ink,
        fontSize: 25,
        fontWeight: 900,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        transform: `scale(${pop})`,
      }}
    >
      {label}
    </div>
  );
};

const CodeCard = () => {
  const frame = useCurrentFrame();
  const lines = [
    "builder.Root = new Sequence",
    "  { new HttpEndpoint(),",
    "    new SendEmail(),",
    "    new WriteLine(\"Done\") };",
  ];

  return (
    <div
      style={{
        width: 510,
        borderRadius: 24,
        background: "#11263a",
        boxShadow: "0 24px 64px rgba(16,34,51,0.25)",
        color: "#dff9ff",
        padding: 28,
        fontFamily: "Menlo, Consolas, monospace",
        fontSize: 24,
        lineHeight: 1.55,
      }}
    >
      <div style={{ display: "flex", gap: 8, marginBottom: 18 }}>
        {[colors.coral, colors.gold, colors.teal].map((color) => (
          <div key={color} style={{ width: 16, height: 16, borderRadius: "50%", background: color }} />
        ))}
      </div>
      {lines.map((line, index) => {
        const opacity = interpolate(frame, [85 + index * 10, 98 + index * 10], [0, 1], clamp);
        return (
          <div key={line} style={{ opacity, whiteSpace: "pre" }}>
            {line}
          </div>
        );
      })}
    </div>
  );
};

const ScreenshotFrame = ({ src, progress }: { src: string; progress: number }) => (
  <div
    style={{
      width: 620,
      height: 366,
      borderRadius: 28,
      background: colors.white,
      padding: 16,
      boxShadow: "0 28px 70px rgba(16,34,51,0.2)",
      border: `1px solid ${colors.line}`,
      overflow: "hidden",
      transform: `translateY(${(1 - progress) * 34}px) scale(${0.96 + progress * 0.04})`,
    }}
  >
    <Img
      src={staticFile(src)}
      style={{
        width: "100%",
        height: "100%",
        objectFit: "cover",
        objectPosition: "left top",
        borderRadius: 18,
      }}
    />
  </div>
);

const RuntimeRail = () => {
  const frame = useCurrentFrame();
  const dotProgress = draw(frame, 210, 280);
  const dotX = 220 + dotProgress * 840;

  return (
    <div
      style={{
        position: "absolute",
        left: 110,
        right: 110,
        top: 348,
        height: 16,
        borderRadius: 999,
        background: "rgba(16,34,51,0.13)",
      }}
    >
      <div
        style={{
          width: `${dotProgress * 100}%`,
          height: "100%",
          borderRadius: 999,
          background: `linear-gradient(90deg, ${colors.teal}, ${colors.gold}, ${colors.coral})`,
        }}
      />
      <div
        style={{
          position: "absolute",
          left: dotX - 22,
          top: -18,
          width: 52,
          height: 52,
          borderRadius: "50%",
          background: colors.white,
          border: `7px solid ${colors.coral}`,
          boxShadow: "0 14px 32px rgba(16,34,51,0.2)",
        }}
      />
    </div>
  );
};

const FeatureCard = ({
  title,
  text,
  color,
  x,
  y,
  delay,
}: {
  title: string;
  text: string;
  color: string;
  x: number;
  y: number;
  delay: number;
}) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [delay, delay + 18], [0, 1], clamp);
  const translateY = rise(frame, delay, 28);

  return (
    <div
      style={{
        position: "absolute",
        left: x,
        top: y,
        width: 312,
        height: 138,
        borderRadius: 24,
        background: "rgba(255,255,255,0.9)",
        borderTop: `8px solid ${color}`,
        boxShadow: "0 18px 38px rgba(16,34,51,0.13)",
        padding: "22px 24px",
        opacity,
        transform: `translateY(${translateY}px)`,
      }}
    >
      <div style={{ fontSize: 27, fontWeight: 900, color: colors.ink, marginBottom: 10 }}>{title}</div>
      <div style={{ fontSize: 20, fontWeight: 700, lineHeight: 1.25, color: colors.slate }}>{text}</div>
    </div>
  );
};

export const ElsaReadmeVideo = () => {
  const frame = useCurrentFrame();
  const heroProgress = draw(frame, 0, 76);
  const titleY = rise(frame, 6, 42);
  const logoScale = spring({ frame: frame - 5, fps: 30, config: { damping: 18, stiffness: 110 } });

  return (
    <AbsoluteFill style={{ fontFamily: "Inter, Arial, sans-serif", color: colors.ink }}>
      <Background />

      <Scene start={0} end={86}>
        {() => (
          <AbsoluteFill>
            <div
              style={{
                position: "absolute",
                left: 86,
                top: 106,
                transform: `translateY(${titleY}px)`,
              }}
            >
              <div style={{ transform: `scale(${logoScale})`, transformOrigin: "left center" }}>
                <LogoMark />
              </div>
              <div
                style={{
                  marginTop: 30,
                  fontSize: 78,
                  lineHeight: 0.95,
                  fontWeight: 950,
                  letterSpacing: 0,
                  maxWidth: 500,
                }}
              >
                Elsa Workflows
              </div>
              <div
                style={{
                  marginTop: 22,
                  fontSize: 31,
                  lineHeight: 1.2,
                  fontWeight: 800,
                  color: colors.slate,
                  maxWidth: 500,
                }}
              >
                A workflow engine that runs inside your .NET applications.
              </div>
            </div>
            <div
              style={{
                position: "absolute",
                right: 62,
                top: 130,
                opacity: interpolate(heroProgress, [0.15, 0.45], [0, 1], clamp),
                transform: `translateX(${interpolate(heroProgress, [0.1, 0.55], [80, 0], { ...clamp, easing: ease })}px)`,
              }}
            >
              <ScreenshotFrame src="http-hello-world-workflow-designer.png" progress={heroProgress} />
            </div>
            <div style={{ position: "absolute", left: 88, bottom: 82, display: "flex", gap: 18 }}>
              <Pill color={colors.teal}>Code</Pill>
              <Pill color={colors.coral}>Designer</Pill>
              <Pill color={colors.gold}>JSON</Pill>
            </div>
          </AbsoluteFill>
        )}
      </Scene>

      <Scene start={76} end={170}>
        {(progress) => (
          <AbsoluteFill>
            <div style={{ position: "absolute", left: 78, top: 74 }}>
              <div style={{ fontSize: 52, fontWeight: 950 }}>Build workflows your way</div>
              <div style={{ fontSize: 28, fontWeight: 800, color: colors.slate, marginTop: 10 }}>
                Code-first when you want control. Visual design when teams need clarity.
              </div>
            </div>
            <div style={{ position: "absolute", left: 86, top: 206 }}>
              <CodeCard />
            </div>
            <div
              style={{
                position: "absolute",
                right: 76,
                top: 192,
                opacity: interpolate(progress, [0.2, 0.55], [0, 1], clamp),
              }}
            >
              <ScreenshotFrame src="http-send-email-workflow-designer.png" progress={progress} />
            </div>
          </AbsoluteFill>
        )}
      </Scene>

      <Scene start={158} end={252}>
        {(progress) => (
          <AbsoluteFill>
            <div style={{ position: "absolute", left: 92, top: 70 }}>
              <div style={{ fontSize: 52, fontWeight: 950 }}>Orchestrate real work</div>
              <div style={{ fontSize: 28, fontWeight: 800, color: colors.slate, marginTop: 10 }}>
                HTTP, schedules, events, messages, and long-running processes.
              </div>
            </div>
            <Connector x1={267} y1={329} x2={506} y2={329} progress={progress} />
            <Connector x1={680} y1={329} x2={918} y2={329} progress={Math.max(0, progress - 0.18) / 0.82} />
            <WorkflowNode label="Trigger" x={110} y={286} color={colors.teal} delay={172} />
            <WorkflowNode label="Decide" x={520} y={286} color={colors.gold} delay={190} />
            <WorkflowNode label="Act" x={930} y={286} color={colors.coral} delay={208} />
            <FeatureCard
              title="Short-running"
              text="Execute direct application tasks."
              color={colors.teal}
              x={118}
              y={474}
              delay={218}
            />
            <FeatureCard
              title="Long-running"
              text="Suspend, resume, and persist."
              color={colors.gold}
              x={484}
              y={474}
              delay={228}
            />
            <FeatureCard
              title="Event-driven"
              text="React to APIs, queues, and time."
              color={colors.coral}
              x={850}
              y={474}
              delay={238}
            />
          </AbsoluteFill>
        )}
      </Scene>

      <Scene start={240} end={330}>
        {(progress) => (
          <AbsoluteFill>
            <div style={{ position: "absolute", left: 94, top: 74 }}>
              <div style={{ fontSize: 52, fontWeight: 950 }}>Embed automation anywhere</div>
              <div style={{ fontSize: 28, fontWeight: 800, color: colors.slate, marginTop: 10 }}>
                Elsa brings orchestration, persistence, and observability to your platform.
              </div>
            </div>
            <RuntimeRail />
            <div style={{ position: "absolute", left: 126, top: 220, opacity: progress }}>
              <Pill color={colors.teal}>ASP.NET Core</Pill>
            </div>
            <div style={{ position: "absolute", left: 430, top: 220, opacity: progress }}>
              <Pill color={colors.gold}>Elsa Studio</Pill>
            </div>
            <div style={{ position: "absolute", left: 716, top: 220, opacity: progress }}>
              <Pill color={colors.coral}>Persistence</Pill>
            </div>
            <div style={{ position: "absolute", left: 986, top: 220, opacity: progress }}>
              <Pill color={colors.teal}>APIs</Pill>
            </div>
            <div
              style={{
                position: "absolute",
                left: 104,
                bottom: 76,
                width: 1040,
                display: "flex",
                alignItems: "center",
                gap: 30,
                opacity: interpolate(progress, [0.45, 0.8], [0, 1], clamp),
                transform: `translateY(${interpolate(progress, [0.45, 0.8], [28, 0], { ...clamp, easing: ease })}px)`,
              }}
            >
              <LogoMark scale={0.72} />
              <div>
                <div style={{ fontSize: 58, lineHeight: 1, fontWeight: 950 }}>Elsa Workflows</div>
                <div style={{ fontSize: 30, color: colors.slate, fontWeight: 800, marginTop: 10 }}>
                  Workflows for builders shipping on .NET.
                </div>
              </div>
            </div>
          </AbsoluteFill>
        )}
      </Scene>
    </AbsoluteFill>
  );
};
